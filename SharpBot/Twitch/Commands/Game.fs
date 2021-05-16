module SharpBot.Twitch.Commands.Game

open System
open System.Threading
open MefBattle.Battle
open MefBattle.Player
open FsToolkit.ErrorHandling
open SharpBot
open SharpBot.Config

let getSpell =
    function
    | "fireball" -> Ok <| WizardSpell Fireball
    | "sheep" -> Ok <| WizardSpell Sheep
    | "attack" -> Ok <| WarriorSpell Attack
    | "stun" -> Ok <| WarriorSpell StunAttack
    | "smite" -> Ok <| HealerSpell Smite
    | "heal" -> Ok <| HealerSpell Heal
    | _ -> Error "Spell Not Found"
    
let getPlayerClass =
    function
    | "wiz" -> Ok <| PlayerClass.Wizard
    | "war" -> Ok <| PlayerClass.Warrior
    | "heal" -> Ok <| PlayerClass.Healer
    | _ -> Error "Class Not Found"
    
type BattleCommandType =
    | Init of int
    | Join of string
    | Action of target: string * spell: string
    | Who of target: string
    | Hp of target: string
    | PlayerActions of target: string
    | GameOver
    
type BattleCommand =
    { Nick: string
      Type: BattleCommandType }
    
let private effectView effect =
    match effect with
    | DamageTaken damage ->
        $"damage {damage}"
    | HealTaken heal ->
        $"heal {heal}"
    | StatusApplied (status, untilTimeSpan) ->
        $"status {status} for {int untilTimeSpan.TotalSeconds}"
    | AddGCD gcd ->
        $"gcd for: {gcd}"
        
let private effectInfoView effectInfo =
    $"{effectInfo.TargetId} => {effectView effectInfo.Effect}"
    
let private statusInfoView now (statusInfo: StatusInfo)  =
    $"status: {statusInfo.Status}, until: {int (now - statusInfo.Until).TotalSeconds}" 
    
let private playerView now player =
    let str = player.StatusInfos |> List.map (statusInfoView now) |> String.concat ", "
    $"Nick: {player.Id}, class: {player.Class}, hp: {player.Hp}, statues: [{str}]"
    
let private spellView =
    function
    | WizardSpell Fireball ->
        "'fireball' 1d10"
    | WizardSpell Sheep ->
        "'sheep'(damage cancel this effect) for 5 mins. "
    | WarriorSpell Attack ->
        "'attack' 2d6"
    | WarriorSpell StunAttack ->
        "'stun' for 1 min"
    | HealerSpell Smite ->
        "'smite' 1d6"
    | HealerSpell Heal ->
        "'heal' 1d6"
    
let private responseToString (response: Response) (now: DateTime) =
    match response with
    | Joined player ->
        playerView now player 
    | EffectInfo effectInfos ->
        effectInfos |> List.map effectInfoView |> String.concat ", "
    | Response.Who player ->
        playerView now player 
    | Response.Hp (nick, hp) ->
        $"{nick} hp: {hp}"
    | Response.GameOver ->
        "Game over"
    | Response.BattleBegins mins ->
        $"Let's the battle begins for {mins} mins"
    | Response.PlayerActions spells ->
        let spellsStr = spells |> List.map spellView |> String.concat " | "
        $"Sample: !attack @{{nick}}. Your actions: {spellsStr}"
        
let private battleErrorToString = function
    | BattleError.AlreadyJoined -> "Already joined"
    | Dead nick -> $"{nick}, you are dead"
    | InvalidSpellForClass -> "Invalid spell"
    | UnableToAction -> "Unable to action"
    | PlayerNotFound nick -> $"{nick} not in the battle"
    | SpellNotFound spell -> $"{spell} not found"
    | ClassNotFound classStr -> $"{classStr} not found"
    | OnGCD (nick, secondsLeft) -> $"{nick} you have GCD, seconds left {secondsLeft}"
    
let join nick playerClassStr battle =
    result {
        let! playerClass = getPlayerClass playerClassStr
        return! join nick playerClass battle |> Result.mapError battleErrorToString
    }
    
let action nick targetNick spellStr reviveAfterMins now battle =
    result {
        let! spell = getSpell spellStr
        return! action nick targetNick spell reviveAfterMins battle now |> Result.mapError battleErrorToString
    }
    
let who nick battle =
    who nick battle |> Result.mapError battleErrorToString
    
let playerAction nick battle =
    playerActions nick battle |> Result.mapError battleErrorToString
    
let myHp nick battle =
    myHp nick battle |> Result.mapError battleErrorToString
    
let private getBattleAndSend (chatMb: MailboxProcessor<ChatCommand>) (oldBattle: Battle) now f =
    match f oldBattle with
    | Ok (Some response, battle) ->
        chatMb.Post (ChatCommand.PrintText (responseToString response now))
        battle
    | Ok (None, battle) -> battle
    | Error e ->
        chatMb.Post (ChatCommand.PrintText e)
        oldBattle
    
let battleMb (config: TwitchConfig) (chatMb: MailboxProcessor<ChatCommand>) = MailboxProcessor.Start(fun inbox ->
    let callback response = 
        match response with
        | Response.GameOver -> inbox.Post { Nick = ""; Type = GameOver }
        | _ -> ()
    
    let rec loop (battleOpt: (Battle * CancellationTokenSource) option) = async {
        let now = DateTime.UtcNow
        let! command = inbox.Receive()

        let updateBattleAndSend battle f =
            getBattleAndSend chatMb battle now f
        
        match command.Type, battleOpt with
        | Init endMins, None ->
            let (responseOpt, newBattle, async) = init (float endMins) callback now
            let cts = new CancellationTokenSource()
            
            responseOpt
            |> Option.iter(fun response -> chatMb.Post (ChatCommand.PrintText (responseToString response now)))
            
            do Async.Start(async, cts.Token)
            
            return! loop (Some (newBattle, cts))
        | Init _, Some b ->
            return! loop (Some b)
        | Join classStr, Some (battle, cts) ->
            return! loop (Some (updateBattleAndSend battle (join command.Nick classStr), cts))
        | Action (target, spellStr), Some (battle, cts) ->
            return! loop (Some (updateBattleAndSend battle (action command.Nick target spellStr config.ReviveAfterMins DateTime.UtcNow), cts))
        | Who target, Some (battle, cts) ->
            return! loop (Some (updateBattleAndSend battle (who target), cts))
        | PlayerActions target, Some (battle, cts) ->
            return! loop (Some (updateBattleAndSend battle (playerAction target), cts))
        | Hp target, Some (battle, cts) ->
            return! loop (Some (updateBattleAndSend battle (myHp target), cts))
        | GameOver, Some (_, cts) ->
            cts.Cancel()
            chatMb.Post (ChatCommand.PrintText (responseToString Response.GameOver now))
            return! loop None
        | _, None ->
            return! loop None
       
    }
    
    loop None)
