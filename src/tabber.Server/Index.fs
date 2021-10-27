module tabber.Server.Index

open Bolero
open Bolero.Html
open Bolero.Server.Html
open tabber

let page = doctypeHtml [] [
    head [] [
        meta [attr.charset "UTF-8"]
        meta [attr.name "viewport"; attr.content "width=device-width, initial-scale=1.0"]
        title [] [text "Tabber"]
        ``base`` [attr.href "/"]
        link [attr.rel "stylesheet"; attr.href "css/index.css"]
        script [attr.src "js/interop.js"][]
        script [attr.src "js/matomo.js"][]
    ]
    body [] [
        nav [attr.classes ["navbar"; "is-dark"]; "role" => "navigation"; attr.aria "label" "main navigation"] [
            div [attr.classes ["navbar-brand"]] [
                a [attr.classes ["navbar-item"; "is-size-5"]; attr.href "/"] [
                    img [attr.style "height:50px"; attr.src "img/logo.png"]
                    text " tabber"
                ]
            ]
        ]
        div [attr.id "pnorco"] [rootComp<tabber.Update.Pnorco>]
        boleroScript
    ]
]