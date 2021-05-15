namespace SharpBot.Twitch

open System
open System.IO
open System.Net.Sockets

type TwitchIrcClient(ip: string, port: int, userName: string, password: string, channel: string) =

    let mutable inputStream: StreamReader = null
    let mutable outputStream: StreamWriter = null

    let mutable tcpClient: TcpClient = null

    do
        tcpClient <- new TcpClient()
        tcpClient.Connect(ip, port)

        inputStream <- new StreamReader(tcpClient.GetStream())
        outputStream <- new StreamWriter(tcpClient.GetStream())

        outputStream.WriteLine($"PASS {password}")
        outputStream.WriteLine($"NICK {userName}")
        outputStream.WriteLine($"USER {userName} 8 * :{userName}")
        outputStream.WriteLine($"JOIN #{channel}")
        outputStream.Flush()

    member this.IP = ip

    member this.SendIrcMessage(message: string) =
        async {
            do! outputStream.WriteLineAsync(message) |> Async.AwaitTask
            do! outputStream.FlushAsync() |> Async.AwaitTask
        }

    member this.SendMessageAsync(message: string) =
        this.SendIrcMessage($":{userName}!{userName}@{userName}.tmi.twitch.tv PRIVMSG #{channel} : {message}")

    member this.ReadMessageAsync() =
        inputStream.ReadLineAsync() |> Async.AwaitTask

    interface IDisposable with
        member _.Dispose() =
            inputStream.Dispose()
            outputStream.Dispose()
            tcpClient.Dispose()


