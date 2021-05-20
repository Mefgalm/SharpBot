module SharpBot.Database.DynamicCommandDb

open LiteDB.FSharp.Extensions
open SharpBot.Commands.DynamicCommand
open Db


let upsert (dynamicCommand: DynamicCommand) =
    let coll = db.GetCollection<DynamicCommand>()
    coll.Upsert (dynamicCommand) |> ignore
    
let remove commandName =
    let coll = db.GetCollection<DynamicCommand>()
    coll.delete <@ fun dc -> dc.Id = commandName @> |> ignore
    
let tryGet commandName =
    let coll = db.GetCollection<DynamicCommand>()
    coll.tryFindOne <@ fun dc -> dc.Id = commandName @>