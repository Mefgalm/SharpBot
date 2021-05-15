module MefBattle.Battle

open System
open System.Threading
open MefBattle.Player
open FsToolkit.ErrorHandling
open Common


type Battle =
    { Players: Player list
      EndDateTime: DateTime }
    
let private gameOverAsync callback (sleep: int) =
    async {
        do! Async.Sleep sleep
        callback Response.GameOver
    }

let init endAfterMins callback (now: DateTime) =
    let sleepMiilis = (int) (endAfterMins * 60. * 1000.)
    
    Some (Response.BattleBegins (int endAfterMins)),    
    { Players = []
      EndDateTime = now.AddMinutes(endAfterMins) },
    gameOverAsync callback sleepMiilis
   
let private checkAlreadyInBattle nick battle =
    if battle.Players |> List.exists(fun x -> x.Id = nick) then
        Error AlreadyJoined
    else
        Ok ()
    
let join nick playerClass battle = result {
    do! checkAlreadyInBattle nick battle
    let player = create nick playerClass
    let newBattle = { battle with Players = player:: battle.Players }
    return (Some <| Response.Joined player, newBattle)
}
        
let private checkPlayer nick battle =
    match battle.Players |> List.tryFind (fun p -> p.Id = nick) with
    | Some player -> Ok player
    | None -> Error <| PlayerNotFound nick
    
let private cancelExpiredStatusesForAllPlayers now battle =
    let cancelExpiredStatusesForPlayer now player =
        let activeStatuses = player.StatusInfos |> List.remove (fun s -> now >= s.Until)
        { player with StatusInfos = activeStatuses }
    
    let activePlayers = battle.Players |> List.map (cancelExpiredStatusesForPlayer now)
    { battle with Players = activePlayers }
    
let private revivePlayers reviveAfterMins now battle =
    let updatePlayer now (player: Player) =
        player.DeathTime
        |> Option.map (fun deathTime ->
            if deathTime.AddMinutes reviveAfterMins < now then
                { player with DeathTime = None
                              Hp = generateHp () } 
            else player)
        |> Option.defaultValue player
    
    { battle with Players = battle.Players |> List.map (updatePlayer now) }
    
let private resetGCD now battle =
    let resetGCD now player =
        let gcd =
            player.GCD
            |> Option.bind(fun gcd ->
                if now > gcd then
                    None
                else
                    Some gcd)
        { player with GCD = gcd }
    
    { battle with Players = battle.Players |> List.map (resetGCD now) }
    
let private preBattleChecks reviveAfterMins now =
    cancelExpiredStatusesForAllPlayers now
    >> revivePlayers reviveAfterMins now
    >> resetGCD now
      
      
let private applyEffectInfo effectInfos battle now =
    let newPlayers =
        List.fold (fun players effectInfo ->
            List.map (fun player ->
                if player.Id = effectInfo.TargetId then
                    applyEffect now player effectInfo.Effect
                else
                    player)
                players)
            battle.Players
            effectInfos
    
    { battle with Players = newPlayers }
    
let private filterEffectsToResponse effectInfos =
    List.remove (fun effectInfo ->
        match effectInfo.Effect with
        | AddGCD _ -> true
        | _ -> false)
        effectInfos
    
let action playerNick targetNick spell reviveAfterMins battle now = result {
    let preBattle = preBattleChecks reviveAfterMins now battle
    
    let! player = preBattle |> checkPlayer playerNick
    let! target = preBattle |> checkPlayer targetNick
    
    let! effectInfos = invoke player spell target now
    let postBattle = applyEffectInfo effectInfos preBattle now
    
    return (Some <| Response.EffectInfo (filterEffectsToResponse effectInfos), postBattle)
}

let who playerNick battle = result {
    let! player = battle |> checkPlayer playerNick
    return (Some <| Who player, battle)
}

let myHp playerNick battle = result {
    let! player = battle |> checkPlayer playerNick
    return (Some <| Hp (playerNick, player.Hp), battle)
}
