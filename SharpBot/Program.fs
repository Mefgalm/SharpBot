open System
open System.IO
open System.Text.RegularExpressions
open SharpBot
open SharpBot.Chat.Twitch

[<EntryPoint>]
let main argv =
    run () |> Async.RunSynchronously
    0