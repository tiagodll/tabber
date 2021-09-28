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
        counter: int
        tab: Tab option
        tabText: string
        tabs: Tab list
        error: string option
    }

and Tab =
    {
        id: string
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
        counter = 0
        tab = None
        tabText = ""
        tabs = []
        error = None
    }

let createTab = { id="new"; title="x"; riffs=[]; sequence=[] }

type Message =
    | SetPage of Page
    | Error of exn
    | ClearError
    | TabsLoaded of Tab[]
    | FileLoaded of InputFileChangeEventArgs
    | SetTabText of string

let saveHabitsToLocalStorage (js:IJSRuntime) habits' =
    js.InvokeVoidAsync("ToStorage", {|label="habits"; value=habits'|}).AsTask() |> ignore

let saveToFilesystem (js:IJSRuntime) data =
    js.InvokeVoidAsync("SaveToFilesystem", {|data=data|}).AsTask() |> ignore

let dateMask = "yyyy-MM-dd"

let LoadCheckins (js:IJSRuntime) (date:DateTime) =
    Cmd.OfJS.either js "FromStorage" [| "tabs_" + date.ToString("yyyy-MM-dd") |] TabsLoaded Error


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
        let riffsPattern = "(?<title>\[Riff \d\])\n(?<content>(?:[GDAE]\|[\-\—\d]*\n)*)"
        // let matched = Regex.Match(text, riffsPattern)
        let mutable m = Regex.Match(text, riffsPattern)
        let mutable tab = {id=""; title=""; riffs=[]; sequence=[]}
        while m.Success do
            let riff = {name=m.Groups.["title"].Value; content=m.Groups.["content"].Value}
            tab <- {tab with riffs = List.append tab.riffs [riff]}
            m <- m.NextMatch()

        let seqPattern = "(?<riff>Riff \d+)x(?<reps>\d*)\n*"
        let mutable m2 = Regex.Match(text, seqPattern)
        while m2.Success do
            let seq = {name=m2.Groups.["riff"].Value; reps=1}//m2.Groups.["reps"].Value}
            tab <- {tab with sequence = List.append tab.sequence [seq]}
            m2 <- m2.NextMatch()
        // //gm
        //let (tab: Tab option) = tryParse text
        {model with tabText=text; tab=Some tab}, Cmd.none
        
 
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
        a [ attr.classes ["icon"; "is-large"; "is-clickable"; "has-text-primary"; "add-habit-button"]; 
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
        div[] [
            h1 [attr.``class`` "title"] [text tab.title]
            ul [attr.classes ["tile"; "is-ancestor"]] [
            // forEach model.tabs <| fun tab ->
            //     li [attr.classes ["tile"; "is-child box"; "habit-dashboard-button"]][
            //         span[][text <| tab.title]
            //         // span[attr.classes ["icon"]][i[attr.classes ["mdi"; "mdi-trash-can"]; on.click (fun x_ -> dispatch (RemoveHabitSent habit))][]]
            //     ]
            ]
        ]

let editPage model dispatch =
    let tab = match model.tab with
                | None -> createTab
                | Some tab -> tab
    div[] [
        h3 [] [text tab.id]
        h1 [attr.``class`` "title"] [text tab.title]
        ul [attr.classes ["tile"; "is-ancestor"]] []
        textarea [attr.``class`` "textarea"; bind.input.string model.tabText (dispatch << SetTabText)][]

        div[attr.classes ["tab"]][
            match model.tab with
            | None -> div[][]
            | Some tab -> 
                div[][
                    span[attr.classes ["title"]] [text tab.title]
                    ul[attr.classes ["riffs"]] [
                        forEach tab.riffs <| fun riff ->
                        li [attr.classes ["riff"]][
                            span[][text riff.name]
                            pre[][text riff.content]
                        ]
                    ]
                    ul[attr.classes ["sequence"]] [
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

type Pnorco() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let init _ = initModel, Cmd.none //Cmd.OfJS.either this.JSRuntime "FromStorage" [| "tabs" |] TabsLoaded Error
        let update = update this.JSRuntime
        Program.mkProgram init update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
