module SharpBot.Twitch.Database.Db

open LiteDB
open LiteDB.FSharp

let mapper = FSharpBsonMapper()
let db = new LiteDatabase("sharpBot.db", mapper)