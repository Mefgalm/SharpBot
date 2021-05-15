namespace SharpBot

open MefBattle.Battle
open MefBattle.Player

[<RequireQualifiedAccess>]
type ChatCommand =
    | Ruin
    | Discord
    | Run of string
    | WhereIsWebCam
    | StartBattle of self: string * int
    | DiceRoll of count: int * power: int
    | DiceRollPlus of count: int * power: int * plus: int
    | JoinBattle of self: string * playerClass: string
    | Cast of self: string * target: string * spell: string
    | Who of self: string
    | Hp of self: string
    | PrintText of string
    | GameOver of self: string
    | AddDynamicCommand of self: string * command: string * content: string
    | RemoveDynamicCommand of self: string * command: string
    | GetDynamicCommandContext of command: string
    
    
    
type Model =
    { RuinCount: int }
    