[<AutoOpen>]
module Common.Functions

open System
open System.Text.RegularExpressions


let (|RegEx|_|) pattern input =
    let m = Regex.Match(input, pattern)

    if m.Success
    then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None
    
let (|Integer|_|) (str: string) =
    match Int32.TryParse(str) with
    | true, value -> Some value
    | _ -> None