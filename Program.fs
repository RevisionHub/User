open CommonLib
let user_db = "/home/harlan/website_ssl/user.db"
let users = 
    try 
        System.IO.File.ReadAllLines(user_db)|>Array.map(fun (i) -> let j = i.Split(',') in j.[0],j.[1]) |> Map.ofArray
    with |e -> failwithf "Could not open the user database:\n%A" e
let conversions = 
    [
        "password",
            (System.Text.ASCIIEncoding.UTF8.GetBytes : string -> byte[])
            >> System.Security.Cryptography.SHA512Cng.Create().ComputeHash
            >> System.Convert.ToBase64String
    ]
    |> Map.ofList<string,(string->string)>
let convertfrompost (s:string) =
    let s = s.Replace('+',' ')
    System.Text.RegularExpressions.Regex("\%").Matches s
    |> Seq.cast
    |> Seq.map (fun (i:System.Text.RegularExpressions.Match) -> i.Index)
    |> Seq.fold(fun s i -> String.concat "" [s.[..i-1]; int("0x"+s.[i+1..i+2])|>char|>string;s.[i+3..]]) s

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
        let vars = 
            v
            |> System.Text.ASCIIEncoding.UTF8.GetString
            |> fun i -> i.Split '&'
            |> Array.map (fun i -> let i' = i.Split('=') in convertfrompost(i'.[0]),convertfrompost(i'.[1]))
            |> Array.map (fun (i,j) -> Map.tryFind i conversions |> function |None -> i,j |Some(k) -> i,k j)
        let vars' = 
            [|
                for i,j in vars do 
                    yield Array.concat
                        [|
                            [|0uy|]
                            System.Text.ASCIIEncoding.UTF8.GetBytes(i)
                            [|0uy|]
                            System.Text.ASCIIEncoding.UTF8.GetBytes(j)
                        |]
            |]
            |> Array.concat
            |> Array.append g
            |> Array.append 
            <| [|0uy|]
        printfn "%A" vars'
        {payloadsize=vars'.Length;payloadverb=PayloadVerb.GotPost;payload=vars'}|>write c.[Daemon.Page]
    printfn "%A" argv
    0 // return an integer exit code

