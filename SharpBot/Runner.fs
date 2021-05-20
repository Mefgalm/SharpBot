module SharpBot.Runner

open MefBattle.Player
open SharpBot
open SharpBot.Commands
open SharpBot.Commands.Game
open SharpBot.Database
open SharpBot.Commands.DynamicCommand
    
let shouldBeOwner nick = "mefgalm" = nick 
    
[<RequireQualifiedAccess>]
type RunnerResponse =
    | Ruin of int
    | Discord of string
    | Run of string
    | DiceRoll of int list * int option * int
    | DynamicCommandContent of string
    | BattleResponse of Response
    
let private runCommand config (battleMb: MailboxProcessor<BattleCommand>) callback chatCommand =
    async {
        match chatCommand with
        | ChatCommand.Ruin ->
            let ruinModel = RuinDb.getRuinModel()
            let newRuinModel = Ruin.ruin ruinModel
            RuinDb.updateRuinModel newRuinModel
            do! callback (RunnerResponse.Ruin newRuinModel.Count)
        | ChatCommand.Run code ->
            do! callback (RunnerResponse.Run <| CSharpRun.runCSharp code)
        | ChatCommand.DiceRoll (count, power) ->
//            Dice.throw chatMb count power None
            do! callback (RunnerResponse.DiceRoll ([], None, 1))
        | ChatCommand.DiceRollPlus (count, power, plus) ->
//            Dice.throw chatMb count power (Some plus)
            do! callback (RunnerResponse.DiceRoll ([], (Some plus), 1))
        | ChatCommand.StartBattle (self, mins) ->
            if shouldBeOwner self then
                battleMb.Post <| Init mins
        | ChatCommand.GameOver self ->
            if shouldBeOwner self then
                battleMb.Post <| GameOver
        | ChatCommand.JoinBattle (self, playerClassStr) ->
            battleMb.Post <| Join (self, playerClassStr)
        | ChatCommand.Cast (self, targetNick, spell) ->
            battleMb.Post <| Action (self, targetNick, spell)
        | ChatCommand.Who target ->
            battleMb.Post <| Who target
        | ChatCommand.Hp target ->
            battleMb.Post <| Hp target
        | ChatCommand.PlayersActions self ->
            battleMb.Post <| PlayerActions self
        | ChatCommand.AddDynamicCommand (self, command, content) ->
            if shouldBeOwner self then
                let dynamicCommand = { Id = command; Content = content }
                DynamicCommandDb.upsert dynamicCommand
        | ChatCommand.RemoveDynamicCommand (self, command) ->
            if shouldBeOwner self then
                DynamicCommandDb.remove command
        | ChatCommand.GetDynamicCommandContext command ->
             match DynamicCommandDb.tryGet command with
             | Some command -> do! callback (RunnerResponse.DynamicCommandContent command.Content)
             | None -> ()
    }
    
let createChatMb (gameMb:MailboxProcessor<BattleCommand>) config callback = MailboxProcessor.Start(fun inbox ->
    let gameCallback response = callback (RunnerResponse.BattleResponse response)
        
    gameMb.Post (BattleCommand.RegisterCallback gameCallback)
    
    let rec loop () =
        async {
            let! command = inbox.Receive()
            do! runCommand config gameMb callback command
            return! loop ()
        }
        
    loop ())