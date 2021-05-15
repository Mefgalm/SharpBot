module MefBattle.Roll

open System


let private random = Random()

let range x = random.Next(1, x + 1)

let diceThrows count power =
    Array.init count (fun _ -> range power)