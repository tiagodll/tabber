module tabber.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components.Forms
open System.Text.RegularExpressions
open System.Web

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Dashboard
    | [<EndPoint "/tab/{id}">] Play of id: string
    | [<EndPoint "/edit/{id}">] Edit of id: string

/// The Elmish application's model.
type Model =
    {
        page: Page
        error: string option
        state: State
    }

and State = {
    play: PlayState option
    dashboard: DashboardState
    edit: EditState option
}
and PlayState = {
    tab: Tab
    currentRiff: string
    riffCounter: int
    repCounter: int
}
and DashboardState = {
    tabs: Tab list
}
and EditState = {
    tab: Tab
    tabText: string
}
and Tab =
    {
        id: string
        band: string
        title: string
        riffs: Riff list
        sequence: Sequence list
    }
and Riff =
    {
        name: string
        content: string   
    }
and Sequence =
    {
        name: string
        reps: int   
    }

let initModel =
    {
        page = Dashboard
        error = None
        state = { play = None; edit = None; dashboard= { tabs = [] } }
    }
// let createPlayState = { currentCount=0; currentRiff="" }
let createTab = { id="new"; band="x"; title="y"; riffs=[]; sequence=[] }

type Message =
    | Init
    | SetPage of Page
    | Error of exn
    | ClearError
    | TabsLoaded of Tab[]
    | FileLoaded of InputFileChangeEventArgs
    | SetTabText of string
    | SaveTab
    | MouseOverSeq of string
    | IncreaseCounter
    | DecreaseCounter
    | ResetCounter
    | Keydown of string

let saveHabitsToLocalStorage (js:IJSRuntime) habits' =
    js.InvokeVoidAsync("ToStorage", {|label="habits"; value=habits'|}).AsTask() |> ignore

let saveToFilesystem (js:IJSRuntime) data =
    js.InvokeVoidAsync("SaveToFilesystem", {|data=data|}).AsTask() |> ignore

let dateMask = "yyyy-MM-dd"

let LoadCheckins (js:IJSRuntime) (date:DateTime) =
    Cmd.OfJS.either js "FromStorage" [| "tabs_" + date.ToString("yyyy-MM-dd") |] TabsLoaded Error


let matchMetadata text =
    let pattern = "^(?<band>(?:\w* *)+) - (?<song>(?:\w* *)+\n)"
    let mutable m = Regex.Match(text, pattern)
    match m.Success with
        | false -> {| band=""; song="" |}
        | true -> {| band=m.Groups.["band"].Value; song=m.Groups.["song"].Value |}

let matchRiffs text =
    let pattern = "(?<title>\[Riff \d\])\n(?<content>(?:[GDAE]\|[\-\—\d]*\n)*)"
    let mutable m = Regex.Match(text, pattern)
    let mutable list = []
    while m.Success do
        let item = {name=m.Groups.["title"].Value; content=m.Groups.["content"].Value}
        list <- List.append list [item]
        m <- m.NextMatch()
    list

let matchSeq text =
    let pattern = "(?<riff>Riff \d+)x(?<reps>\d*)\n*"
    let mutable m = Regex.Match(text, pattern)
    let mutable list = []
    while m.Success do
        let item = {name=m.Groups.["riff"].Value; reps=m.Groups.["reps"].Value |> int}
        list <- List.append list [item]
        m <- m.NextMatch()
    list

type KeydownCallback(f: string -> unit) =
    [<JSInvokable>]
    member this.Invoke(arg1) = f (arg1)

let ofKeyDown f = DotNetObjectReference.Create(KeydownCallback(f))

let update (js:IJSRuntime) message model =
    let setupJSCallback = 
        Cmd.ofSub (fun dispatch -> 
            let onKeydown k = dispatch (Keydown k)
            js.InvokeVoidAsync("initOnKeyDownCallback", ofKeyDown onKeydown).AsTask() |> ignore
        )

    // let onSignIn = function
    //     | Some _ -> Cmd.ofMsg GetTabs
    //     | None -> Cmd.none
    match message with
    | Init -> 
        js.InvokeVoidAsync("Log", ["## INIT ###"]).AsTask() |> ignore
        model, setupJSCallback
    | SetPage page ->
        match page with
        | Edit id -> 
            let edit' = match id with
                        | "new" -> { tab=createTab; tabText="" }
                        | _ ->
                            let tab' = List.find (fun x -> x.id=id) model.state.dashboard.tabs
                            { tab = tab'; tabText = "implement the tab.toText" }
            { model with page = page; state={model.state with edit=Some edit'} }, Cmd.none
        | Play id ->
            let play' = match model.state.dashboard.tabs |> List.tryFind (fun x -> x.id=id) with
                        | None -> { tab=createTab; currentRiff = ""; riffCounter = 0; repCounter = 0 }
                        | Some tab -> { tab = tab; currentRiff = ""; riffCounter = 0; repCounter = 0 }
            { model with page = page; state={model.state with play=Some play'} }, Cmd.none
        | _ -> { model with page = page }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none

    | TabsLoaded tabs ->
        // {model with tabs = tabs}, Cmd.none
        // {model with tabs = []}, Cmd.none
        model, Cmd.none

    | FileLoaded file ->
        js.InvokeVoidAsync("Log", [file]).AsTask() |> ignore
        // let tab' = file.File;
        // {model with tab = tab'}, Cmd.none
        
        model, Cmd.none

    | SetTabText text ->
        match  model.state.edit with
        | None -> model, Cmd.none
        | Some edit ->
            let metadata = matchMetadata text
            let tab = {
                id = HttpUtility.UrlEncode(metadata.band + metadata.song + DateTime.Now.Millisecond.ToString())
                band = metadata.band
                title = metadata.song
                riffs = matchRiffs text
                sequence = matchSeq text
            }
            let editState = {edit with tabText=text; tab=tab}
            {model with state={model.state with edit=Some editState}}, Cmd.none
    
    | IncreaseCounter ->
        match model.state.play with
        | None -> model, Cmd.none
        | Some play -> 
            let (riffc', repc') = match play.riffCounter, play.repCounter, play.tab.sequence.Item(play.riffCounter).reps with
                                    | (riffc, repc, reps) when repc + 2 > reps -> (riffc + 1, 0)
                                    | (riffc, repc, reps) -> (riffc, repc + 1)

            let riff = play.tab.sequence.Item(riffc').name
            let play' = {play with currentRiff=riff; riffCounter=riffc'; repCounter=repc'}
            let state' = {model.state with play=Some play'}
            {model with state=state'}, Cmd.none
    | DecreaseCounter ->
        match model.state.play with
        | None -> model, Cmd.none
        | Some play -> 
            let riff = play.tab.sequence.Item(play.riffCounter-1).name
            let play' = {play with currentRiff=riff; riffCounter=play.riffCounter-1; repCounter=0}
            let state' = {model.state with play=Some play'}
            {model with state=state'}, Cmd.none
    | ResetCounter ->
        match model.state.play with
        | None -> model, Cmd.none
        | Some play -> 
            let riff = play.tab.sequence.Item(0).name
            let play' = {play with currentRiff=riff; riffCounter=0; repCounter=0}
            let state' = {model.state with play=Some play'}
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
            {model with state={model.state with dashboard={model.state.dashboard with tabs=tabs'}}; page=Dashboard}, Cmd.none

    | Keydown key ->
        js.InvokeVoidAsync("Log", [key]).AsTask() |> ignore
        match key with
        | "ArrowLeft" | "ArrowUp" -> model, Cmd.ofMsg IncreaseCounter
        | "ArrowRight" | "ArrowDown" -> model, Cmd.ofMsg DecreaseCounter
        | _ -> model, Cmd.ofMsg ResetCounter
        
 
let router = Router.infer SetPage (fun model -> model.page)

let dashboardPage (model:Model) dispatch =
    div [] [
        ul [attr.classes ["tile"; "is-ancestor"]] [
            forEach model.state.dashboard.tabs <| fun tab ->
                li [attr.classes ["tile"; "is-child box"; "habit-dashboard-button-zero"; "disable-select"]][
                        a[attr.href (router.Link (Play tab.id))][text <| tab.title]
                ]
        ]
        span [] [text "xxxxxxxxxxx"]
        // input[attr.``type`` "file"; on.change (fun args -> dispatch (FileLoaded args))]
        //ifile
        a [ //attr.classes ["icon"; "is-large"; "is-clickable"; "has-text-primary"; "add-habit-button"]; 
            attr.href (router.Link <| Edit "new")][
                i[attr.classes["mdi"; "mdi-48px"; "mdi-plus-circle"]][]
        ]
    ]

let formField (labelText:string) (control) =
    div [attr.``class`` "field"] [
        label [attr.``class`` "label"] [text labelText]
        div [attr.``class`` "control"] [control]
    ]

let playPage model dispatch =
    match model.state.play with
    | None -> div[][ text "no tab selected" ]
    | Some play ->
        div[attr.classes ["tab"]][
            span[][text <| "counter: " + play.riffCounter.ToString() + " # " + play.repCounter.ToString()]
            button[on.click (fun _ -> dispatch DecreaseCounter)][text " - "]
            button[on.click (fun _ -> dispatch IncreaseCounter)][text " + "]
            button[on.click (fun _ -> dispatch ResetCounter)][text " 0 "]
            br[]
            span[][text play.tab.id]
            br[]
            span[attr.classes ["title"]] [text play.tab.band]
            span[][text " - "]
            span[attr.classes ["title"]] [text play.tab.title]
            br[]
            // ul[][
            //     play.tab.riffs
            //     |> List.map (fun x -> li[][text x.name])
            // ]
            ul[attr.classes ["riffs"]] [
                let makeLi (clas:string) (riff: Riff option) =
                    match riff with
                    | None -> li[][ text "nothing"]
                    | Some r -> 
                        li [attr.classes ["riff"; clas]; attr.id r.name][
                            span[][text <| "[" + r.name + "]"]
                            pre[][text r.content]
                        ]

                li[][text play.tab.sequence.[play.riffCounter].name]

                play.tab.riffs
                |> List.tryFind (fun (x:Riff) -> x.name = play.tab.sequence.[play.riffCounter].name)
                |> makeLi "current"
                
                play.tab.riffs
                |> List.tryFind (fun (x:Riff) -> x.name = play.tab.sequence.[play.riffCounter+1].name)
                |> makeLi "next"
            ]
            ul[attr.classes ["sequence"] ] [
                let mutable counter=0;
                forEach play.tab.sequence <| fun seq ->
                //let name = HttpUtility.UrlEncode(seq.name)
                let selected = match counter - play.riffCounter with
                                        | 0 -> "selected"
                                        | _ -> ""
                counter <- counter + 1 //seq.reps

                li [
                    attr.classes ["riff"; selected]
                    attr.id seq.name
                    on.click (fun _ -> dispatch <| MouseOverSeq (seq.name))
                ][
                    span[][text seq.name]
                    span[][text " x "]
                    span[][text <| seq.reps.ToString()]
                ]
            ]
        ]

let editPage model dispatch =
    match model.state.edit with
    | None -> div[][ text "no tab selected" ]
    | Some edit ->
        div[] [
            h3 [] [text edit.tab.id]
            ul [attr.classes ["tile"; "is-ancestor"]] []
            textarea [attr.``class`` "textarea"; bind.input.string edit.tabText (dispatch << SetTabText)][]
            button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch SaveTab)][text "save"]

            // input [attr.``type`` "text"; bind.input.string model.tab.id (dispatch << SetTitle)]
            div[attr.classes ["tab"]][
                div[][
                    span[][text edit.tab.id]
                    br[]
                    span[attr.classes ["title"]] [text edit.tab.band]
                    span[][text " - "]
                    span[attr.classes ["title"]] [text edit.tab.title]
                    br[]
                    ul[attr.classes ["riffs"]] [
                        forEach edit.tab.riffs <| fun riff ->
                        li [attr.classes ["riff"]][
                            span[][text riff.name]
                            pre[][text riff.content]
                        ]
                    ]
                    ul[attr.classes ["sequence"] ] [
                        forEach edit.tab.sequence <| fun seq ->
                        li [attr.classes ["riff"]][
                            span[][text seq.name]
                            span[][text " x "]
                            span[][text <| seq.reps.ToString()]
                        ]
                    ]
                ]
            ]
        ]

let errorNotification err clear =
    div [attr.classes ["notification"; "is-warning"]] [
        button [attr.classes ["delete"]; on.click clear ] []
        span [] [text err]
    ]

let view model dispatch =
    div [attr.``class`` "columns"] [
        div[][text " x "]
        div[attr.``class`` "has-text-centered"][
            // button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch PreviousDay)][text "<"]
            //span[attr.classes ["dashboard-date"]][text (model.date.ToString("yyyy-MM-dd"))]
            // button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch NextDay)][text ">"]
        ]
        // button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch SaveCheckins)][ text "Save"]

        section [attr.``class`` "section"] [
            // BODY
            cond model.page <| function
            | Dashboard -> dashboardPage model dispatch
            | Edit id -> editPage model dispatch
            | Play id -> playPage model dispatch
            //notification
            div [attr.id "notification-area"] [
                match model.error with
                | None -> empty
                | Some err -> errorNotification err (fun _ -> dispatch ClearError)
            ]
        ]
    ]

type Pnorco() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let init _ = initModel, Cmd.ofMsg Init //Cmd.OfJS.either this.JSRuntime "FromStorage" [| "tabs" |] TabsLoaded Error
        let update = update this.JSRuntime

        Program.mkProgram init update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
