module MefBattle.Player

open System
open FsToolkit.ErrorHandling
open Common

type WizardSpell =
    | Fireball
    | Sheep

type WarriorSpell =
    | Attack
    | StunAttack

type HealerSpell =
    | Smite
    | Heal

type Spell =
    | WizardSpell of WizardSpell
    | WarriorSpell of WarriorSpell
    | HealerSpell of HealerSpell

type PlayerClass =
    | Wizard
    | Warrior
    | Healer

type PlayerStatus =
    | Sap
    | Stun

type StatusInfo =
    { Status: PlayerStatus
      Until: DateTime }

type Effect =
    | DamageTaken of int
    | HealTaken of int
    | StatusApplied of PlayerStatus * TimeSpan
    | AddGCD of int

type Player =
    { Id: string
      StatusInfos: StatusInfo list
      Hp: int
      GCD: DateTime option
      Class: PlayerClass
      DeathTime: DateTime option }
    
type EffectInfo =
    { TargetId: string
      Effect: Effect }

type BattleError =
    | InvalidSpellForClass
    | UnableToAction
    | Dead of self: string
    | PlayerNotFound of nick: string
    | AlreadyJoined
    | SpellNotFound of spell: string
    | ClassNotFound of classStr: string
    | OnGCD of self: string * secondsLeft: int

[<RequireQualifiedAccess>]
type BattleResponse =
    | BattleBegins of int
    | Joined of Player
    | EffectInfo of EffectInfo list
    | Who of Player
    | Hp of nick: string * int
    | PlayerActions of Spell list
    | GameOver
    | BattleError of BattleError
    
let generateHp () =
    Roll.diceThrows 4 6 |> Array.sort |> Array.tail |> Array.sum
    
let create nick playerClass =
    { Id = nick
      StatusInfos = []
      Hp = generateHp ()
      GCD = None
      Class = playerClass
      DeathTime = None }
    
let private validateCast playerClass spell =
    match playerClass, spell with
    | PlayerClass.Wizard, WizardSpell _ -> Ok ()
    | PlayerClass.Warrior, WarriorSpell _ -> Ok ()
    | PlayerClass.Healer, HealerSpell _ -> Ok ()
    | _ -> Error InvalidSpellForClass
    
let private (<*>) count power =
    Roll.diceThrows count power |> Array.sum
    
let private cast player target spell =
    let createEffectInfo targetId effects =
        { TargetId = targetId
          Effect = effects }
    
    let gcdEffect = createEffectInfo player.Id (AddGCD 60)
    
    let targetEffects =
        let ce = createEffectInfo target.Id
        match spell with
        | WizardSpell Fireball ->    [ ce <| DamageTaken (1 <*> 10) ] 
        | WizardSpell Sheep ->       [ ce <| StatusApplied (PlayerStatus.Sap, TimeSpan.FromMinutes 5.) ]
        
        | WarriorSpell Attack ->     [ ce <| DamageTaken (2 <*> 6) ]
        | WarriorSpell StunAttack -> [ ce <| StatusApplied (PlayerStatus.Stun, TimeSpan.FromMinutes 1.) ]
        
        | HealerSpell Heal ->        [ ce <| HealTaken (1 <*> 6) ]
        | HealerSpell Smite ->       [ ce <| DamageTaken (1 <*> 6) ]

    [ yield gcdEffect
      yield! targetEffects ]
    
let private isEffectAllowActing now effect  =
    match effect.Status with
    | Sap | Stun when now < effect.Until -> false
    | _ -> true
    
let isAlive hp = hp > 0
let isDead = not << isAlive
    
let private validatePlayerEnableToActing player now =
   if player.StatusInfos |> List.forall (isEffectAllowActing now) then
       Ok ()
   else
       Error UnableToAction
       
let private shouldBeAlive player =
    if isAlive player.Hp then
        Ok ()
    else
        Error (Dead player.Id)
        
let private checkGCD (now: DateTime) player =
    match player.GCD with
    | Some gcd -> Error (OnGCD <| (player.Id, int (gcd - now).TotalSeconds))
    | None -> Ok ()
    
let applyEffect (now: DateTime) player effect =
    match effect with
    | HealTaken value ->
        { player with Hp = player.Hp + value
                      DeathTime = None }
    | DamageTaken value ->
        let newHp = player.Hp - value
        { player with Hp = newHp
                      StatusInfos = player.StatusInfos |> List.remove(fun s -> s.Status = Sap)
                      DeathTime = if isDead newHp then Some now else None }
    | StatusApplied (status, untilTimeStamp) ->
        { player with StatusInfos = { Status = status; Until = now + untilTimeStamp }::player.StatusInfos}
    | AddGCD gcd ->
        { player with GCD = Some (now.AddSeconds (float gcd)) } 
    
let applyEffects player effects now =
    effects |> List.fold (applyEffect now) player
    
let getSpell =
    function
    | "fireball" -> Ok <| WizardSpell Fireball
    | "sheep" -> Ok <| WizardSpell Sheep
    | "attack" -> Ok <| WarriorSpell Attack
    | "stun" -> Ok <| WarriorSpell StunAttack
    | "smite" -> Ok <| HealerSpell Smite
    | "heal" -> Ok <| HealerSpell Heal
    | x -> Error <| BattleError.SpellNotFound x
    
    
let invoke player spellStr target now = result {
    let! spell = getSpell spellStr
    do! validateCast player.Class spell
    do! validatePlayerEnableToActing player now
    do! shouldBeAlive player
    do! checkGCD now player
    return cast player target spell
}

