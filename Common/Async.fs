module Common.Async

open System.Threading.Tasks

let completedTask = Task.CompletedTask |> Async.AwaitTask

