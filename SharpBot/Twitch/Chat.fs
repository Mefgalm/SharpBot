module SharpBot.Chat.Twitch

open System
open System.Text.RegularExpressions
open Common
open MefBattle.Player
open Newtonsoft.Json
open SharpBot
open SharpBot.Config
open SharpBot.Twitch
open System.IO
open SharpBot.Commands
open SharpBot.Commands.Game
open SharpBot.Database
open SharpBot.Runner

let toLower (str: string) = str.ToLower() 

let private parseMessage =
    let regex =
        Regex(":([\d\w]+)![\w\d@\.]+ PRIVMSG #[\d\w]+ :(.+)")

    fun (str: string) ->
        if isNull str then
            None
        else
            let m = regex.Match(str)

            if m.Success
            then Some(toLower m.Groups.[1].Value, toLower m.Groups.[2].Value)
            else None
   
let chatCallback (client: TwitchIrcClient) (runnerResponse: RunnerResponse) =
    client.SendMessageAsync (Views.runnerResponseToString runnerResponse DateTime.UtcNow)

let private pinger (client: TwitchIrcClient) =
    let rec loop () =
        async {
            do! client.SendIrcMessage($"PING {client.IP}")
            do! Async.Sleep(TimeSpan.FromMinutes 5.)
            return! loop ()
        }

    loop ()

let createChatCommand (nick, (msg: string)) =
    let cc t = Some t
    if msg.StartsWith("!") then
        match msg with
        | RegEx "!руина" _ ->
            cc ChatCommand.Ruin        
        | RegEx "!run (.+)" [ code ] ->
            cc <| ChatCommand.Run code        
        | RegEx "!who +@([\w_]+)" [target] ->
            cc <| ChatCommand.Who (toLower target) 
        | RegEx "!hp +@([\w_]+)" [target] ->
            cc <| ChatCommand.Hp (toLower target)
        | RegEx "!who" _ ->
            cc <| ChatCommand.Who nick
        | RegEx "!hp" _ ->
            cc <| ChatCommand.Hp nick
        | RegEx "!add-([a-zA-ZА-Яа-я]+) +(.+)" [command; content] ->
            cc <| ChatCommand.AddDynamicCommand (nick, command, content)
        | RegEx "!remove-([a-zA-ZА-Яа-я]+)" [command] ->
            cc <| ChatCommand.RemoveDynamicCommand (nick, command)        
        | RegEx "!battle (\d+)" [ (Integer mins) ] ->
            cc <| ChatCommand.StartBattle (nick, mins)
        | RegEx "!(\d+)d(\d+) *\+ *(\d+)?" [ (Integer count); (Integer power); (Integer plus) ] ->
            cc <| ChatCommand.DiceRollPlus(count, power, plus)
        | RegEx "!(\d+)d(\d+)" [ (Integer count); (Integer power) ] ->
            cc <| ChatCommand.DiceRoll(count, power)
        | RegEx "!join +(\w+)" [ playerClass ] ->
            cc <| ChatCommand.JoinBattle (nick, playerClass)
        | RegEx "!rules" [] ->
            cc <| ChatCommand.PlayersActions nick
        | RegEx "!(\w+) +@([\w_]+)" [ spellName; targetNick ] ->
            cc <| ChatCommand.Cast(nick, toLower targetNick, toLower spellName)
        | RegEx "!([a-zA-ZА-Яа-я]+)" [command] ->
            cc <| ChatCommand.GetDynamicCommandContext command
        | _ -> None
    else
        None

let reader (chatMb: MailboxProcessor<ChatCommand>) (client: TwitchIrcClient) =
    let rec loop () = async {
        let! msg = client.ReadMessageAsync()
        
        msg
        |> parseMessage
        |> Option.bind createChatCommand
        |> Option.iter (chatMb.Post)
        
        return! loop ()
    }
    
    loop ()

let private initDb () =
    try 
        RuinDb.insertRuinModel { Id = 1; Count = 0 }
    with _ -> ()

let private loadConfig () =
    let configText = File.ReadAllText "appsettings.json"
    JsonConvert.DeserializeObject<TwitchConfig> (configText)

let run () =
    async {
        initDb()
        
        let config = loadConfig()
        
        let gameMb = Game.battleMb config.ReviveAfterMins
        
        gameMb.Post (BattleCommand.RegisterCallback (fun response ->
            match response with
            | BattleResponse.BattleBegins _ ->
                printfn "Start update"
            | BattleResponse.GameOver ->
                printfn "End update"
            | _ ->
                printfn "Update"
            
            async.Return ()))
        
        use twitchIrcClient = new TwitchIrcClient("irc.twitch.tv", 6667, "botomef", config.OAuth, "mefgalm")
        let chat = Runner.createChatMb gameMb config (chatCallback twitchIrcClient)
        
        do! Async.StartChild (pinger twitchIrcClient) |> Async.Ignore
        do! reader chat twitchIrcClient
    }



