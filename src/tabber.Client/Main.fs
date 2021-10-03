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
open Microsoft.AspNetCore.Components.Web
// open FSharpPlus

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Dashboard
    | [<EndPoint "/tab/{id}">] Play of id: string
    | [<EndPoint "/edit/{id}">] Edit of id: string

/// The Elmish application's model.
type Model =
    {
        page: Page
        tab: Tab option
        tabText: string
        tabs: Tab list
        error: string option
        state: State
    }

and State = {
    play: PlayState
}
and PlayState = {
    currentRiff: string
    counter: int
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
        tab = None
        tabText = ""
        tabs = []
        error = None
        state = { play = { counter=0; currentRiff="" } }
    }
// let createPlayState = { currentCount=0; currentRiff="" }
let createTab = { id="new"; band="x"; title="y"; riffs=[]; sequence=[] }

type Message =
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
    | Keydown of KeyboardEventArgs

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

// let keyDownEvent e = 

let update (js:IJSRuntime) message model =
    // let onSignIn = function
    //     | Some _ -> Cmd.ofMsg GetTabs
    //     | None -> Cmd.none
    match message with
    | SetPage page ->
        { model with page = page }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none

    | TabsLoaded tabs ->
        // {model with tabs = tabs}, Cmd.none
        {model with tabs = []}, Cmd.none

    | FileLoaded file ->
        js.InvokeVoidAsync("Log", [file]).AsTask() |> ignore
        // let tab' = file.File;
        // {model with tab = tab'}, Cmd.none
        
        model, Cmd.none

    | SetTabText text ->
        let metadata = matchMetadata text
        let tab = {
            id = HttpUtility.UrlEncode(metadata.band + metadata.song + DateTime.Now.Millisecond.ToString())
            band = metadata.band
            title = metadata.song
            riffs = matchRiffs text
            sequence = matchSeq text
        }
        {model with tabText=text; tab=Some tab}, Cmd.none
    
    | IncreaseCounter ->
        let riff = match model.tab with
                    | None -> ""
                    | Some tab -> "[" + tab.sequence.Item(model.state.play.counter+1).name + "]"
        let play' = {model.state.play with currentRiff=riff; counter=model.state.play.counter+1}
        let state' = {model.state with play=play'}
        {model with state=state'}, Cmd.none
    | DecreaseCounter ->
        let riff = match model.tab with
                    | None -> ""
                    | Some tab -> "[" + tab.sequence.Item(model.state.play.counter-1).name + "]"
        let play' = {model.state.play with currentRiff=riff; counter=model.state.play.counter-1}
        let state' = {model.state with play=play'}
        {model with state=state'}, Cmd.none
    | ResetCounter ->
        let riff = match model.tab with
                    | None -> ""
                    | Some tab -> "[" + tab.sequence.Item(0).name + "]"
        let play' = {model.state.play with currentRiff=riff; counter=0}
        let state' = {model.state with play=play'}
        {model with state=state'}, Cmd.none

    | MouseOverSeq name ->
        js.InvokeVoidAsync("Log", [name]).AsTask() |> ignore
        let play' = {model.state.play with currentRiff=name}
        let state' = {model.state with play=play'}
        {model with state=state'}, Cmd.none

    | SaveTab ->
        let tabs' = match model.tab with
                    | None -> model.tabs
                    | Some tab -> List.append model.tabs [tab]
        {model with tabs=tabs'; page=Dashboard}, Cmd.none

    | Keydown e ->
        js.InvokeVoidAsync("Log", [e.Code; e.Key; e.Repeat.ToString(); e.Type]).AsTask() |> ignore
        model, Cmd.none
        
 
let router = Router.infer SetPage (fun model -> model.page)

let dashboardPage (model:Model) dispatch =
    div [] [
        ul [attr.classes ["tile"; "is-ancestor"]] [
            forEach model.tabs <| fun tab ->
                li [attr.classes ["tile"; "is-child box"; "habit-dashboard-button-zero"; "disable-select"]][
                        a[attr.href (router.Link (Play tab.title))][text <| tab.title]
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
    match model.tab with
    | None -> div[][ text "no tab selected" ]
    | Some tab ->
        div[attr.classes ["tab"]][
            span[][text <| "counter: " + model.state.play.counter.ToString() + " "]
            button[on.click (fun _ -> dispatch DecreaseCounter)][text " - "]
            button[on.click (fun _ -> dispatch IncreaseCounter)][text " + "]
            button[on.click (fun _ -> dispatch ResetCounter)][text " 0 "]
            br[]
            span[][text tab.id]
            br[]
            span[attr.classes ["title"]] [text tab.band]
            span[][text " - "]
            span[attr.classes ["title"]] [text tab.title]
            br[]
            ul[attr.classes ["riffs"]] [
                forEach tab.riffs <| fun (riff: Riff) ->
                let selected = match String.Compare(riff.name, model.state.play.currentRiff) with
                                        | 0 -> "selected"
                                        | _ -> ""
                li [attr.classes ["riff"; selected]
                    attr.id riff.name
                    ][
                    span[][text riff.name]
                    pre[][text riff.content]
                ]
            ]
            ul[attr.classes ["sequence"] ] [
                let mutable counter=0;
                forEach tab.sequence <| fun seq ->
                //let name = HttpUtility.UrlEncode(seq.name)
                let selected = match counter - model.state.play.counter with
                                        | 0 -> "selected"
                                        | _ -> ""
                counter <- counter + 1 //seq.reps

                li [
                    attr.classes ["riff"; selected]
                    attr.id seq.name
                    on.click (fun _ -> dispatch <| MouseOverSeq ("["+seq.name+"]"))
                ][
                    span[][text seq.name]
                    span[][text " x "]
                    span[][text <| seq.reps.ToString()]
                ]
            ]
        ]

let editPage model dispatch =
    let tab = match model.tab with
                | None -> createTab
                | Some tab -> tab
    div[] [
        h3 [] [text tab.id]
        ul [attr.classes ["tile"; "is-ancestor"]] []
        textarea [attr.``class`` "textarea"; bind.input.string model.tabText (dispatch << SetTabText)][]
        button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch SaveTab)][text "save"]

        // input [attr.``type`` "text"; bind.input.string model.tab.id (dispatch << SetTitle)]
        div[attr.classes ["tab"]][
            match model.tab with
            | None -> div[][]
            | Some tab -> 
                div[][
                    span[][text tab.id]
                    br[]
                    span[attr.classes ["title"]] [text tab.band]
                    span[][text " - "]
                    span[attr.classes ["title"]] [text tab.title]
                    br[]
                    ul[attr.classes ["riffs"]] [
                        forEach tab.riffs <| fun riff ->
                        li [attr.classes ["riff"]][
                            span[][text riff.name]
                            pre[][text riff.content]
                        ]
                    ]
                    ul[attr.classes ["sequence"] ] [
                        forEach tab.sequence <| fun seq ->
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
// open System.Timers
// let subscription = 
//     Cmd.ofSub <| fun dispatch ->
//         let timer = new Timer(100.)
//         timer.add_Elapsed(fun _ e ->
//             dispatch (Tick e.SignalTime))
//         timer.Start()

type Pnorco() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let init _ = initModel, Cmd.none //Cmd.OfJS.either this.JSRuntime "FromStorage" [| "tabs" |] TabsLoaded Error
        let update = update this.JSRuntime
        Program.mkProgram init update view
        |> Program.withRouter router
        // |> Program.withSubscription subscription
#if DEBUG
        |> Program.withHotReload
#endif
