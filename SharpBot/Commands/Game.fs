module SharpBot.Commands.Game

open System
open System.Threading
open MefBattle.Battle
open MefBattle.Player
open FsToolkit.ErrorHandling
open SharpBot

    
type BattleCommand =
    | RegisterCallback of callback: (BattleResponse -> Async<unit>)
    | Init of int
    | Join of self: string * playerClass: string
    | Action of self: string * target: string * spell: string
    | Who of target: string
    | Hp of target: string
    | PlayerActions of target: string
    | GameOver
    

let private sendCallbacks callbacks response =
    callbacks
    |> List.map (fun c -> c response)
    |> Async.Parallel
    |> Async.Ignore
    
let private getBattleAndSend callbacks (oldBattle: Battle) f = async {
        match f oldBattle with
        | Ok (Some response, battle) ->
            do! sendCallbacks callbacks response
            return battle
        | Ok (None, battle) ->
            return battle
        | Error e ->
            do! sendCallbacks callbacks (BattleResponse.BattleError e)
            return oldBattle
    }
    
let battleMb reviveAfterMins = MailboxProcessor.Start(fun inbox ->
    let callback response = 
        match response with
        | BattleResponse.GameOver -> inbox.Post GameOver
        | _ -> ()
    
    let rec loop (battleOpt: (Battle * CancellationTokenSource) option) callbacks = async {
        let now = DateTime.UtcNow
        let! command = inbox.Receive()

        let updateBattleAndSend battle f =
            getBattleAndSend callbacks battle f
        
        match command, battleOpt with
        | RegisterCallback callback, bc ->
            return! loop bc (callback::callbacks)
        | Init endMins, None ->
            let (responseOpt, newBattle, async) = init (float endMins) callback now
            let cts = new CancellationTokenSource()
            
            match responseOpt with
            | Some response -> do! sendCallbacks callbacks response
            | None -> ()
            
            do Async.Start(async, cts.Token)
            
            return! loop (Some (newBattle, cts)) callbacks
        | Init _, Some b ->
            return! loop (Some b) callbacks
        | Join (self, classStr), Some (battle, cts) ->
            let! battle = updateBattleAndSend battle (join self classStr)
            return! loop (Some (battle, cts)) callbacks
        | Action (self, target, spellStr), Some (battle, cts) ->
            let! battle = updateBattleAndSend battle (action self target spellStr reviveAfterMins DateTime.UtcNow)
            return! loop (Some (battle, cts)) callbacks
        | Who target, Some (battle, cts) ->
            let! battle = updateBattleAndSend battle (who target)
            return! loop (Some (battle, cts)) callbacks
        | PlayerActions target, Some (battle, cts) ->
            let! battle = updateBattleAndSend battle (playerActions target)
            return! loop (Some (battle, cts)) callbacks
        | Hp target, Some (battle, cts) ->
            let! battle = updateBattleAndSend battle (myHp target)
            return! loop (Some (battle, cts)) callbacks
        | GameOver, Some (_, cts) ->
            cts.Cancel()
            do! sendCallbacks callbacks <| BattleResponse.GameOver 
            return! loop None callbacks
        | _, None ->
            return! loop None callbacks
       
    }
    
    loop None [])
