module SharpBot.Twitch.Views

open System
open MefBattle.Player
open SharpBot.Runner

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
    $"Nick: {player.Id}, class: {player.Class}, hp: {player.Hp}, statuses: [{str}]"
    
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
    
let private battleErrorToString = function
    | BattleError.AlreadyJoined -> "Already joined"
    | Dead nick -> $"{nick}, you are dead"
    | InvalidSpellForClass -> "Invalid spell"
    | UnableToAction -> "Unable to action"
    | PlayerNotFound nick -> $"{nick} not in the battle"
    | SpellNotFound spell -> $"{spell} not found"
    | ClassNotFound classStr -> $"{classStr} not found"
    | OnGCD (nick, secondsLeft) -> $"{nick} you have GCD, seconds left {secondsLeft}"
    
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
    | Response.BattleError error ->
        battleErrorToString error
    
let runnerResponseToString (runnerResponse:  RunnerResponse) (now: DateTime) =
    match runnerResponse with
    | RunnerResponse.Ruin count -> $"Всего руин {count}"  
    | RunnerResponse.Discord discord -> discord
    | RunnerResponse.Run code -> code
    | RunnerResponse.DiceRoll _ -> "diceroll" 
    | RunnerResponse.DynamicCommandContent content -> content
    | RunnerResponse.BattleResponse response -> responseToString response now