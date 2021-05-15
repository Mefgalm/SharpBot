module SharpBot.Twitch.Commands.Ruin

[<CLIMutable>]
type RuinModel =
    { Id: int
      Count: int }

let ruin ruinModel =
    let newRuinModel = { ruinModel with Count = ruinModel.Count + 1 }
    (newRuinModel, $"Всего руин {newRuinModel.Count}")