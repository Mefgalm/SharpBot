module SharpBot.Commands.Ruin

[<CLIMutable>]
type RuinModel =
    { Id: int
      Count: int }

let ruin ruinModel =
    { ruinModel with Count = ruinModel.Count + 1 }
