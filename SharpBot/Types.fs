namespace SharpBot

open MefBattle.Battle
open MefBattle.Player

[<RequireQualifiedAccess>]
type ChatCommand =
    | Ruin
    | Run of string
    | StartBattle of self: string * int
    | DiceRoll of count: int * power: int
    | DiceRollPlus of count: int * power: int * plus: int
    | JoinBattle of self: string * playerClass: string
    | Cast of self: string * target: string * spell: string
    | Who of self: string
    | Hp of self: string
    | GameOver of self: string
    | AddDynamicCommand of self: string * command: string * content: string
    | RemoveDynamicCommand of self: string * command: string
    | GetDynamicCommandContext of command: string
    | PlayersActions of self: string
    
type Model =
    { RuinCount: int }
    