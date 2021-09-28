namespace tabber.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Bolero.Remoting.Client

module Program =

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.Pnorco>("#main")
        builder.Services.AddRemoting(builder.HostEnvironment) |> ignore
        builder.Build().RunAsync() |> ignore
        0
