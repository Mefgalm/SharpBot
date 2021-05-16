module SharpBot.Twitch.Runner

open SharpBot
open SharpBot.Config
open SharpBot.Twitch.Commands
open SharpBot.Twitch.Commands.Game
open SharpBot.Twitch.Database
open SharpBot.Twitch.Commands.DynamicCommand
    
let shouldBeOwner (config: TwitchConfig) nick = config.Owner = nick 
    
let private runCommand config chatMb (battleMb: MailboxProcessor<BattleCommand>) callback chatCommand =
    let cb nick t = { Nick = nick; Type  = t  }
    async {
        match chatCommand with
        | ChatCommand.Ruin ->
            let ruinModel = RuinDb.getRuinModel()
            let newRuinModel, msg = Ruin.ruin ruinModel
            RuinDb.updateRuinModel newRuinModel
            do! callback msg
        | ChatCommand.Discord ->
            do! callback (Discord.discord)
        | ChatCommand.WhereIsWebCam ->
            do! callback (WhereIsWebCam.whereIsWebCam)
        | ChatCommand.Run code ->
            do! callback (CSharpRun.runCSharp code)
        | ChatCommand.DiceRoll (count, power) ->
            Dice.throw chatMb count power None
        | ChatCommand.DiceRollPlus (count, power, plus) ->
            Dice.throw chatMb count power (Some plus)
        | ChatCommand.StartBattle (self, mins) ->
            if shouldBeOwner config self then
                battleMb.Post <| cb self (Init mins)
        | ChatCommand.GameOver self ->
            if shouldBeOwner config self then
                battleMb.Post <| cb self GameOver
        | ChatCommand.JoinBattle (self, playerClassStr) ->
            battleMb.Post <| cb self (Join playerClassStr)
        | ChatCommand.Cast (self, targetNick, spell) ->
            battleMb.Post <| cb self (Action (targetNick, spell))
        | ChatCommand.Who target ->
            battleMb.Post <| cb "" (Who target)
        | ChatCommand.Hp target ->
            battleMb.Post <| cb "" (Hp target)
        | ChatCommand.AddDynamicCommand (self, command, content) ->
            if shouldBeOwner config self then
                let dynamicCommand = { Id = command; Content = content }
                DynamicCommandDb.upsert dynamicCommand
        | ChatCommand.RemoveDynamicCommand (self, command) ->
            if shouldBeOwner config self then
                DynamicCommandDb.remove command
        | ChatCommand.GetDynamicCommandContext command ->
             match DynamicCommandDb.tryGet command with
             | Some command -> do! callback command.Content
             | None -> ()
        | ChatCommand.PlayersActions self ->
            battleMb.Post <| cb "" (PlayerActions self)
        | ChatCommand.PrintText text ->
            do! callback text         
    }
    
let createChatMb config callback = MailboxProcessor.Start(fun inbox ->
    let gameMb = Game.battleMb config inbox 
       
    let rec loop () =
        async {
            let! command = inbox.Receive()
            do! runCommand config inbox gameMb callback command
            return! loop ()
        }
        
    loop ())