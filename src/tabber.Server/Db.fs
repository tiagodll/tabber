module Db                 
open System
open System.Data.SQLite

let migrate =
    let connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    let connection = new SQLiteConnection(connectionString)
    connection.Open()
    
    [|
    """CREATE TABLE Tabs (
    id TEXT,
    band TEXT,
    title TEXT,
    content TEXT,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)"""
    
    """CREATE TABLE Users (   
    email TEXT,
    name TEXT,                              
    password TEXT,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)"""
    
    "INSERT INTO Users(email, name, password) VALUES ('tiago@dalligna.com', 'tiago', 'asd')"
    |]
    |> Array.iter (fun x -> (new SQLiteCommand(x, connection)).ExecuteNonQuery() |> ignore)

    connection.Close()