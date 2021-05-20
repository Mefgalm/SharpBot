module SharpBot.Commands.CSharpRun

open DynamicExpresso

let runCSharp =
    let inter = Interpreter()
    fun code ->
        try
            (inter.Eval code).ToString()
        with e -> e.Message
    