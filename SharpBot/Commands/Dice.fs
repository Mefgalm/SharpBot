module SharpBot.Commands.Dice

open FsToolkit.ErrorHandling
open SharpBot

let validateCount value =
    if value > 0 && value <= 100 then Some value else None

let validatePower value =
    if value > 0 && value <= 100 then Some value else None

let dice countInt powerInt plusOpt =
    option {
        let! count = validateCount countInt
        let! power = validateCount powerInt
        
        let diceThrows = Domain.Dice.diceThrows count power
        let result = Array.sum diceThrows 
        let diceThrowsStr =
            diceThrows
            |> Array.map (fun x -> x.ToString())
            |> String.concat " + "

        let msg =
            match plusOpt with
            | Some plus -> $"{diceThrowsStr} (+ {plus}) = {result + plus}"
            | None -> $"{diceThrowsStr} = {result}"

        return msg
    }
    
//let throw (chatMb: MailboxProcessor<ChatCommand>) countInt powerInt plusOpt =
//    match dice countInt powerInt plusOpt with
//    | Some msg -> chatMb.Post (ChatCommand.PrintText msg)
//    | None -> ()