﻿open CommonLib
[<EntryPoint>]
let main argv = 
    let c = 
        Array.zip
        <| Array.map enum<Daemon> [|0..7|]
        <| [|
            for i = 0 to 7 do 
                let p = new System.IO.Pipes.NamedPipeServerStream("/tmp/" + Daemon.GetName(typeof<Daemon>,enum<Daemon> i) + "_Copelands_2016")
                yield p
            |]
        |> Map.ofArray
    let p = c.[Daemon.User]
    while true do
        let payload = (read p).payload
        let pos = Array.findIndex((=) 0uy) payload
        let g,v = Array.splitAt pos payload |> fun (i,j) -> i,j.[1..] // in v.[..v.Length-2]
        v
        |> System.Text.ASCIIEncoding.UTF8.GetString
        |> fun i -> i.Split '&'
        |> Array.map(fun i -> let i' = i.Split('=') in System.Web.HttpUtility.HtmlDecode(i'.[0]),System.Web.HttpUtility.HtmlDecode(i'.[1]))
        |> printfn "%A"
        {payloadsize=g.Length+1;payloadverb=PayloadVerb.Get;payload=g}|>write c.[Daemon.Page]
    printfn "%A" argv
    0 // return an integer exit code

