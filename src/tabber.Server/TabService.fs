namespace tabber.Server

open System
open System.IO
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting.Server
open System.Data.SQLite
open System.Collections.Generic
open Tabber.Shared.Model
open Tabber.Shared.TabParser

type TabService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<tabber.Update.TabService>()

    let connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    let connection = new SQLiteConnection(connectionString)

    override this.Handler =
        {
            init = fun () -> async {
                let structureSql =
                    "CREATE TABLE IF NOT EXISTS Tabs (" +    
                    "id TEXT, " +
                    "band TEXT, " +
                    "title TEXT, " + 
                    "content TEXT, " + 
                    "timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)"
                           
                let structureCommand = new SQLiteCommand(structureSql, connection)
                connection.Open()
                structureCommand.ExecuteNonQuery() |> ignore
                connection.Close()
            }

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

            signIn = fun (username, password) -> async {
                if username = "tiago" && password = "asd" then
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