namespace tabber.Server

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open tabber

type TabService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<tabber.Update.TabService>()

    let tabsFolder = Environment.GetEnvironmentVariable("TABS_FOLDER")
    let tabs = System.IO.Directory.GetFiles(tabsFolder, "*.btab")

    override this.Handler =
        {
            getLatestTabs = fun () -> async {
                return tabs
                    // |> Array.map Path.GetFull
                    |> Array.map File.ReadAllText
            }

            // addTab = ctx.Authorize <| fun tab -> async {
            addTab = fun (name, tab) -> async {

                Path.Combine(Environment.GetEnvironmentVariable("TABS_FOLDER"), name + ".btab")
                |> fun filepath -> File.WriteAllText (filepath, tab) 
                |> ignore
            }

            removeTab = fun id -> async {
                Path.Combine(Environment.GetEnvironmentVariable("TABS_FOLDER"), id + ".btab")
                |> File.Delete
            }

            signIn = fun (username, password) -> async {
                if password = "password" then
                    do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
                    return Some username
                else
                    return None
            }

            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            getUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }