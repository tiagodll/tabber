module Db                 
open System
open System.Data.SQLite

type MigrationItem = {
    version: int
    content: string
}
let connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")

let getCurrentVersion connection =
    let selectCommand = new SQLiteCommand("SELECT value FROM Config WHERE label='DbVersion'", connection)
    let v = selectCommand.ExecuteScalar()
    match v with
        | null -> 
            "INSERT INTO Config (label, value) VALUES('DbVersion', '0');"
            |> (fun x -> (new SQLiteCommand(x, connection)).ExecuteNonQuery())
            |> ignore
            0
        | x -> x.ToString() |> int

let applyMigration connection item =
    let cmd = new SQLiteCommand(item.content, connection)
    cmd.ExecuteNonQuery() |> ignore

    let cmdVersion = new SQLiteCommand("UPDATE Config SET value=@version WHERE label='DbVersion'", connection)
    cmdVersion.Parameters.AddWithValue("@version", item.version) |> ignore
    cmdVersion.ExecuteNonQuery() |> ignore

let migrate =
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    "CREATE TABLE IF NOT EXISTS Config (label TEXT, value TEXT, timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)"
    |> (fun x -> (new SQLiteCommand(x, connection)).ExecuteNonQuery())
    |> ignore

    let currentVersion = getCurrentVersion connection
    
    [|
        {version=1; content="""
        CREATE TABLE Tabs (
        id TEXT,
        band TEXT,
        title TEXT,
        content TEXT,
        timestamp DATETIME DEFAULT CURRENT_TIMESTAMP);
    
        CREATE TABLE Users (   
        email TEXT,
        name TEXT,                              
        password TEXT,
        timestamp DATETIME DEFAULT CURRENT_TIMESTAMP);
    
        INSERT INTO Users(email, name, password) VALUES ('tiago@dalligna.com', 'tiago', 'asd')
        """}
    |]
    |> Array.filter (fun x -> x.version > currentVersion)
    |> Array.iter (applyMigration connection)

    connection.Close()