namespace tabber.Server

open System
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting.Server
open System.Data.SQLite
open System.Collections.Generic
open Tabber.Shared.Model
open Tabber.Shared.TabParser
open Tabber

type TabService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.TabService.TabService>()

    let connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    let connection = new SQLiteConnection(connectionString)

    override this.Handler =
        {
            getLatestTabs = fun () -> async {
                let result = new List<string>()
                let selectSql = "SELECT * FROM Tabs ORDER BY timestamp DESC LIMIT 10"
                let selectCommand = new SQLiteCommand(selectSql, connection)
                
                connection.Open()
                let reader = selectCommand.ExecuteReader()
                while reader.Read() do
                    result.Add(reader.["content"].ToString())
                                    
                connection.Close()
                return result.ToArray()
            }

            //addTab = ctx.Authorize <| fun (tab) -> async {
            addTab = fun (tab) -> async {
                
                let insertSql = 
                    "INSERT INTO Tabs(id, band, title, content) " + 
                    "VALUES (@id, @band, @title, @content)"

                let text = tab.band + " - " + tab.title + "\n" + (riffsToString tab.riffs) + "\n" + (seqToString tab.sequence)

                use command = new SQLiteCommand(insertSql, connection)       
                command.Parameters.AddWithValue("@id", tab.id) |> ignore
                command.Parameters.AddWithValue("@band", tab.band) |> ignore
                command.Parameters.AddWithValue("@title", tab.title) |> ignore
                command.Parameters.AddWithValue("@content", text) |> ignore
                
                connection.Open()
                command.ExecuteNonQuery() |> ignore
                connection.Close()
            }

            removeTab = fun id -> async {
                let insertSql = "DELETE FROM Tabs WHERE id=@id"
                use command = new SQLiteCommand(insertSql, connection)
                command.Parameters.AddWithValue("@id", id) |> ignore

                connection.Open()
                command.ExecuteNonQuery() |> ignore
                connection.Close()
            }

            signIn = fun (email, password) -> async {
                let mutable result = None
                let selectSql = "SELECT * FROM Users WHERE email=@email AND password=@password"
                let command = new SQLiteCommand(selectSql, connection) 
                command.Parameters.AddWithValue("@email", email) |> ignore
                command.Parameters.AddWithValue("@password", password) |> ignore
                
                connection.Open()
                let reader = command.ExecuteReader()
                if reader.Read() then
                    result <- Some {email=email; name=reader.["name"].ToString()}
                    do! ctx.HttpContext.AsyncSignIn(email, TimeSpan.FromDays(365.))
                connection.Close()

                return result
            }

            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            signUp = fun (signUpRequest) -> async {
                return None
            }

            getUser = ctx.Authorize <| fun () -> async {
                let mutable result = None
                let selectSql = "SELECT * FROM Users WHERE email=@email"
                let command = new SQLiteCommand(selectSql, connection) 
                command.Parameters.AddWithValue("@email", ctx.HttpContext.User.Identity.Name) |> ignore
                
                connection.Open()
                let reader = command.ExecuteReader()
                if reader.Read() then
                    result <- Some {email=reader.["email"].ToString(); name=reader.["name"].ToString()}
                connection.Close()

                return result
            }
        }