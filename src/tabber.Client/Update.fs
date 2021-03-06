module tabber.Update

open Elmish
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Client
open Microsoft.JSInterop

open Tabber.Shared.Model
open tabber.Model
open tabber.Router
open tabber.View
open tabber.TabParserClient
open Tabber.Client.TabService
open Tabber.Client

let saveTabsToLocalStorage (js:IJSRuntime) tab' =
    js.InvokeVoidAsync("ToStorage", {|label="tabs"; value=tab'|}).AsTask() |> ignore

let addTab remote tab =
    Cmd.OfAsync.either remote.addTab tab (fun () -> TabAdded) Error

let removeTab remote name =
    Cmd.OfAsync.either remote.removeTab name (fun () -> TabServerDeleted) Error

let dateMask = "yyyy-MM-dd"

let loadTabs (js:IJSRuntime) =
    Cmd.OfJS.either js "FromStorage" [| "tabs" |] TabsLoaded Error

let loadLatestTabs remote =
    Cmd.OfAsync.either remote.getLatestTabs () LatestTabsLoaded Error


type KeydownCallback(f: string -> unit) =
    [<JSInvokable>]
    member this.Invoke(arg1) = f (arg1)

let ofKeyDown f = DotNetObjectReference.Create(KeydownCallback(f))

let update (js:IJSRuntime) remote message model =
    let setupJSCallback = 
        Cmd.ofSub (fun dispatch -> 
            let onKeydown k = dispatch (Keydown k)
            js.InvokeVoidAsync("initOnKeyDownCallback", ofKeyDown onKeydown).AsTask() |> ignore
        )

    js.InvokeVoidAsync("Log", ["## " + message.ToString() + " ###"]).AsTask() |> ignore

    match message with
    | Init -> 
        model, Cmd.batch [ setupJSCallback; loadTabs js; loadLatestTabs remote; Cmd.ofMsg (AuthMsg Auth.GetSignedInAs) ]
    | SetPage page ->
        match page with
        | Edit id -> 
            let edit' = match id with
                        | "new" -> { tab=emptyTab; tabText="" }
                        | _ ->
                            let tab' = List.find (fun x -> x.id=id) model.state.dashboard.tabs
                            { tab = tab'; tabText = "implement the tab.toText" }
            { model with page = page; state={model.state with edit=Some edit'} }, Cmd.none
        | Play id ->
            let play' = match model.state.dashboard.tabs |> List.tryFind (fun x -> x.id=id) with
                        | None -> 
                            match model.state.dashboard.latestTabs |> List.tryFind (fun x -> x.id=id) with
                            | None -> Id ""   //search online ?
                            | Some tab -> PlayState { tab = tab; currentRiff = ""; riffCounter = 0; repCounter = 0 }
                        | Some tab -> PlayState { tab = tab; currentRiff = ""; riffCounter = 0; repCounter = 0 }

            { model with page = page; state={model.state with play=play'} }, Cmd.none
        | _ -> 
            { model with page = page }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none

    | LatestTabsLoaded tabs ->
        let tabs' = tabs
                    |> Array.toList
                    |> List.map textToTab
        let dashboard' = {model.state.dashboard with latestTabs = tabs'}
        let state' = {model.state with dashboard = dashboard'}    
        {model with state=state'}, Cmd.none

    | AddTab tab ->
        model, addTab remote tab
    | TabAdded ->
        model, Cmd.none

    | DeleteTab tab ->
        let tabs' = model.state.dashboard.tabs
                    |> List.except [tab]
        
        tabs'
            |> tabToString
            |> saveTabsToLocalStorage js
            |> ignore

        let dashboard' = {model.state.dashboard with tabs = tabs'}
        let state' = {model.state with dashboard = dashboard'}    
        {model with state=state'}, Cmd.none

    | DeleteServerTab tab ->
        let name = tab.band + " - " + tab.title
        model, removeTab remote name

    | TabServerDeleted ->
        model, loadLatestTabs remote

    | TabsLoaded tabs ->
        let tabs' = tabs
                    |> Array.toList
                    |> List.map textToTab
        
        tabs' |> List.iter (fun v -> js.InvokeVoidAsync("Log", [v.id]).AsTask() |> ignore)

        let dashboard' = {model.state.dashboard with tabs = tabs'}
        let play' = match model.state.play with
                    | PlayState play -> PlayState play
                    | Id id -> 
                        match tabs' |> List.tryFind (fun x -> x.id=id) with
                        | None -> 
                            match model.state.dashboard.latestTabs |> List.tryFind (fun x -> x.id=id) with
                            | None -> Id ""   //search online ?
                            | Some tab -> PlayState { tab = tab; currentRiff = ""; riffCounter = 0; repCounter = 0 }
                        | Some tab -> PlayState { tab = tab; currentRiff = ""; riffCounter = 0; repCounter = 0 }
                    
        let state' = {model.state with dashboard = dashboard'; play=play'}    
        {model with state=state'}, Cmd.none

    | SetTabText text ->
        match  model.state.edit with
        | None -> model, Cmd.none
        | Some edit ->
            let editState = {edit with tabText=text; tab=textToTab text}
            {model with state={model.state with edit=Some editState}}, Cmd.none
    
    | IncreaseCounter ->
        match model.state.play with
        | Id s -> model, Cmd.none
        | PlayState play -> 
            let (riffc', repc') = match play.riffCounter, play.repCounter, play.tab.sequence.Item(play.riffCounter).reps with
                                    | (riffc, repc, reps) when repc + 2 > reps -> (riffc + 1, 0)
                                    | (riffc, repc, _) -> (riffc, repc + 1)

            let riff = play.tab.sequence.Item(riffc').name
            let play' = PlayState {play with currentRiff=riff; riffCounter=riffc'; repCounter=repc'}
            let state' = {model.state with play=play'}
            {model with state=state'}, Cmd.none
    | DecreaseCounter ->
        match model.state.play with
        | Id s -> model, Cmd.none
        | PlayState play -> 
            let riff = play.tab.sequence.Item(play.riffCounter-1).name
            let play' = PlayState {play with currentRiff=riff; riffCounter=play.riffCounter-1; repCounter=0}
            let state' = {model.state with play=play'}
            {model with state=state'}, Cmd.none
    | ResetCounter ->
        match model.state.play with
        | Id str -> model, Cmd.none
        | PlayState play -> 
            let riff = play.tab.sequence.Item(0).name
            let play' = PlayState {play with currentRiff=riff; riffCounter=0; repCounter=0}
            let state' = {model.state with play=play'}
            {model with state=state'}, Cmd.none

    | MouseOverSeq name ->
        js.InvokeVoidAsync("Log", [name]).AsTask() |> ignore
        // let play' = {model.state.play with currentRiff=name}
        // let state' = {model.state with play=play'}
        // {model with state=state'}, Cmd.none
        model, Cmd.none

    | SaveTab ->
        match model.state.edit with
        | None -> model, Cmd.none
        | Some edit ->
            let tabs' = List.append model.state.dashboard.tabs [edit.tab]

            tabs'
            |> tabToString
            |> saveTabsToLocalStorage js
            |> ignore
            
            {model with state={model.state with dashboard={model.state.dashboard with tabs=tabs'}}; page=Dashboard}, Cmd.none

    | Keydown key ->
        js.InvokeVoidAsync("Log", [key]).AsTask() |> ignore
        match key with
        | "ArrowRight" | "ArrowUp" -> model, Cmd.ofMsg IncreaseCounter
        | "ArrowLeft" | "ArrowDown" -> model, Cmd.ofMsg DecreaseCounter
        | _ -> model, Cmd.ofMsg ResetCounter

    | AuthMsg msg' ->
        match msg' with
        | Auth.Msg.RecvSignUp x ->
            match x with
            | None -> model, Cmd.ofMsg (SetPage Dashboard)
            | Some e -> 
                let res', cmd' = Auth.update remote msg' model.auth
                { model with auth = res' }, Cmd.map AuthMsg cmd'
                //model, Cmd.ofMsg (SetPage Dashboard) // todo: not redirect when return error

        | Auth.Msg.RecvSignIn user ->
            { model with page=Dashboard; auth = {
                model.auth with signedInAs = user; signIn={
                    model.auth.signIn with signInFailed=Option.isNone user}}}, Cmd.none
        | _ -> 
            let res', cmd' = Auth.update remote msg' model.auth
            { model with auth = res' }, Cmd.map AuthMsg cmd'

type Pnorco() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let tabsService = this.Remote<TabService>()
        let init _ = initModel, Cmd.ofMsg Init //Cmd.OfJS.either this.JSRuntime "FromStorage" [| "tabs" |] TabsLoaded Error
        let update = update this.JSRuntime tabsService

        Program.mkProgram init update view
        |> Program.withRouter router

