module Tabber.Client.TabService

open Bolero.Remoting
open Tabber.Shared.Model

type TabService = 
    {
        getLatestTabs: unit -> Async<string[]>
        addTab: Tab -> Async<unit>
        removeTab: string -> Async<unit>

        signIn : string * string -> Async<User option>
        signUp : SignUpRequest -> Async<string option>
        getUser : unit -> Async<User option>
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/tabs"