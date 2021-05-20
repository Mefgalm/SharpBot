open SharpBot.Chat.Twitch

[<EntryPoint>]
let main argv =
    //TODO add discord, whereIsWebcam to DynamicCommands at start
    //TODO add hug: hug {name}
    //TODO rename MefBattle -> Battle
    //TODO !commands -> link (maybe just text for now)
    
    run () |> Async.RunSynchronously
    0