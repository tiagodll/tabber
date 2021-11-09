module tabber.TabParserClient 

open System.Text.RegularExpressions
open System.Web
open Tabber.Shared.Model
                                  
let matchMetadata text =
    let pattern = "^(?<band>(?:\w* *)+) - (?<song>(?:\w* *)+\n)"
    let mutable m = Regex.Match(text, pattern)
    match m.Success with
        | false -> {| band=""; song="" |}
        | true -> {| band=m.Groups.["band"].Value; song=m.Groups.["song"].Value |}
                                  
let matchRiffs text =
    let pattern = "\[(?<title>(?:.+))\]\n(?<content>(?:[GDAE]\|[\-\—\d\|\s]*\n)*)"
    let mutable m = Regex.Match(text, pattern)
    let mutable list = []
    while m.Success do
        let item = {name=m.Groups.["title"].Value; content=m.Groups.["content"].Value}
        list <- List.append list [item]
        m <- m.NextMatch()
    list
                                  
let matchSeq text =
    let pattern = "(?<riffd>.*(?=[x|X]\d+))[x|X](?<reps>\d*)|(?<riff>.+)"
    let mutable m = Regex.Match(text, pattern)
    let mutable list = []
    while m.Success do
        let riff', reps' = match m.Groups.["riffd"].Value with
                            | "" -> m.Groups.["riff"].Value.Trim(), 1
                            | _ -> m.Groups.["riffd"].Value.Trim(), m.Groups.["reps"].Value |> int
        let item = {name=riff'; reps=reps'}
        list <- List.append list [item]
        m <- m.NextMatch()
    list
                                  
let riffsToString (riffs:Riff list) =
    riffs
    |> List.map (fun x -> "[" + x.name + "]\n" + x.content)
    |> String.concat "\n"
                                  
let seqToString (seq:Sequence list) =
    seq
    |> List.map (fun x -> match x.reps with
                            | 1 -> x.name + "\n"
                            | _ -> x.name + "x" + x.reps.ToString() + "\n")
    |> List.fold (fun acc x -> acc + x) "[Sequence]\n"
                                  
let tabToString (tabs:Tab list) =
    tabs
        |> List.map (fun x -> x.band + " - " + x.title + "\n" + (riffsToString x.riffs) + "\n" + (seqToString x.sequence) )
                                  
let textToTab (text: string) =
    let split = text.Split("[Sequence]")
    let metadata = matchMetadata text
    {
        id = HttpUtility.UrlEncode(metadata.band + "-" + metadata.song)// + DateTime.Now.Millisecond.ToString())
        band = metadata.band
        title = metadata.song
        riffs = matchRiffs split.[0]
        sequence = matchSeq (split.[1].Substring(split.[1].IndexOf("\n")))
    }