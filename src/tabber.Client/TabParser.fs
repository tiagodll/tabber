module tabber.TabParser

open System.Text.RegularExpressions
open System.Web

open tabber.Model

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
    let pattern = "(?<riff>.+)[\sx|\sX|x|X](?<reps>\d*)\n+"
    let mutable m = Regex.Match(text, pattern)
    let mutable list = []
    while m.Success do
        let reps' = match m.Groups.["reps"].Value with
                    | "" -> 1
                    | _ -> m.Groups.["reps"].Value |> int
        let item = {name=m.Groups.["riff"].Value; reps=reps'}
        list <- List.append list [item]
        m <- m.NextMatch()
    list

let riffsToString (riffs:Riff list) =
    riffs
    |> List.map (fun x -> "[" + x.name + "]\n" + x.content)
    |> String.concat "\n"

let seqToString (seq:Sequence list) =
    seq
    |> List.map (fun x -> x.name + "x" + x.reps.ToString() + "\n")
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
        sequence = matchSeq split.[1]
    }