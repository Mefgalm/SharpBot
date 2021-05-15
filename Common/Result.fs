[<AutoOpen>]
module Common.Result

let get = function
    | Ok x -> x
    | Error x -> x