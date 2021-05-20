module SharpBot.Database.RuinDb

open LiteDB.FSharp.Extensions
open SharpBot.Commands.Ruin
open Db


let getRuinModel () =
    let coll = db.GetCollection<RuinModel>()
    coll.findOne <@ fun _ -> true @>

let updateRuinModel (ruinModel: RuinModel) =
    let coll = db.GetCollection<RuinModel>()
    coll.Update (ruinModel) |> ignore
    
let insertRuinModel (ruinModel: RuinModel) =
    let coll = db.GetCollection<RuinModel>()
    coll.Insert ruinModel |> ignore
