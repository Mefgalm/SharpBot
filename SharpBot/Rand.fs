module SharpBot.Rand

open System

let random = Random()

let range x = random.Next(1, x + 1)
