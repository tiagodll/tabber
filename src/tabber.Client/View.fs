module tabber.View

open Bolero.Html

open tabber.Model
open tabber.Router

let dashboardPage (model:Model) dispatch =
    div [] [
        ul [attr.classes ["list"]] [
            forEach model.state.dashboard.tabs <| fun tab ->
                li [attr.classes ["link"]][
                        a[attr.href (router.Link (Play tab.id))][text <| tab.title]
                ]
        ]
        a [
            attr.href (router.Link <| Edit "new")][
                i[attr.classes["mdi"; "mdi-48px"; "mdi-plus-circle"]][]
        ]
        br[]
        span[][text "latest on the server"]
        ul [attr.classes ["list"]] [
            forEach model.state.dashboard.latestTabs <| fun tab ->
                li [attr.classes ["link"]][ text <| tab.band + " - " + tab.title
                        // a[attr.href (router.Link (Play tabName.id))][text <| tabName.title]
                ]
        ]
    ]

let formField (labelText:string) (control) =
    div [attr.``class`` "field"] [
        label [attr.``class`` "label"] [text labelText]
        div [attr.``class`` "control"] [control]
    ]

let playPage model dispatch =
    match model.state.play with
    | Id id when id = "" -> div[][ text "Invalid id" ]
    | Id _ -> div[][ text "loading..." ]
    | PlayState play ->
        div[attr.classes ["tab"]][
            span[attr.classes ["title"]] [text <| play.tab.band + " - " + play.tab.title]
            span[][text <| "counter: " + play.riffCounter.ToString() + " # " + play.repCounter.ToString()]
            button[on.click (fun _ -> dispatch DecreaseCounter)][text " - "]
            button[on.click (fun _ -> dispatch IncreaseCounter)][text " + "]
            button[on.click (fun _ -> dispatch ResetCounter)][text " 0 "]

            div[attr.classes ["tab-container"]][
                ul[attr.classes ["riffs"]] [
                    let makeLi (clas:string) (riff: Riff option) =
                        match riff with
                        | None -> li[][ text "nothing"]
                        | Some r -> 
                            li [attr.classes ["riff"; clas]; attr.id r.name][
                                span[][text <| "[" + r.name + "]"]
                                pre[][text r.content]
                            ]

                    play.tab.riffs
                    |> List.tryFind (fun (x:Riff) -> x.name = play.tab.sequence.[play.riffCounter].name)
                    |> makeLi "current"
                    
                    if play.riffCounter < play.tab.sequence.Length - 1 then
                        play.tab.riffs
                        |> List.tryFind (fun (x:Riff) -> x.name = play.tab.sequence.[play.riffCounter+1].name)
                        |> makeLi "next"
                ]
                div[attr.classes ["riff-counter"]][
                    (play.tab.sequence.[play.riffCounter].reps - play.repCounter).ToString()
                    |> text
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
        ]

let editPage model dispatch =
    match model.state.edit with
    | None -> div[][ text "no tab selected" ]
    | Some edit ->
        div[attr.classes ["tag-edditor"]] [
            h3 [] [text edit.tab.id]
            ul [attr.classes ["tile"]] []
            textarea [attr.``class`` "textarea"; bind.input.string edit.tabText (dispatch << SetTabText)][]
            button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch SaveTab)][text "save"]

            button[attr.classes ["button"; "is-small"]; on.click (fun _ -> dispatch <| AddTab edit.tab)][text "upload to server"]

            // input [attr.``type`` "text"; bind.input.string model.tab.id (dispatch << SetTitle)]
            div[attr.classes ["tab-preview"]][
                span[attr.classes ["title"]] [text <| edit.tab.band + " - " + edit.tab.title]
                span[][text edit.tab.id]
                div[attr.classes ["tab-container"]][
                    ul[attr.classes ["riffs"]] [
                        forEach edit.tab.riffs <| fun riff ->
                        li [attr.classes ["riff"; "current"]][
                            span[][text <| "[" + riff.name + "]"]
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
    section [] [
        
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