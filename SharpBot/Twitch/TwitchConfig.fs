module SharpBot.Config

[<CLIMutable>]
type TwitchConfig =
    { OAuth: string
      Owner: string
      ReviveAfterMins: float }