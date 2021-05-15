module SharpBot.Domain.Dice

open System
open FsToolkit.ErrorHandling
open SharpBot.Rand

let diceThrows count power =
    Array.init count (fun _ -> range power)