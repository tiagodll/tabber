module tabber.Router

open Bolero
open tabber.Model

let router : Router<Page, Model, Message> =
    {
        getEndPoint = fun m -> m.page
        
        setRoute = fun path ->
            match path.Trim('/').Split('/') with
            | [||] -> Some Dashboard
            | [|"play"; id|] -> Some (Play (string id))
            | [|"edit"; id|] -> Some (Edit (string id))
            | _ -> None
            |> Option.map SetPage
        
        getRoute = function
            | Dashboard -> "/"
            | Play(id) -> sprintf "/play/%s" id
            | Edit(id) -> sprintf "/edit/%s" id
    }