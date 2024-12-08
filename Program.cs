using System.Collections.ObjectModel;
using CodeCrafters.Interpreter.Parsing;
using CodeCrafters.Interpreter.Runtime;
using CodeCrafters.Interpreter.Scanning;
using CodeCrafters.Interpreter.Syntax.Statements;
using CodeCrafters.Interpreter.Utils;

if (args is [] or ["-r", not null])
{

    REPL repl = args.Length is 2 ? new REPL(args[1]) : new REPL();
    try
    {
        await repl.BeginREPL();
        Environment.Exit(0);
    }
    catch (Exception e)
    {
        await Console.Error.WriteLineAsync($"[{e.GetType().Name}]: {e.Message}\n{e.StackTrace}");
        Environment.Exit(70);    // Software error
        throw;
    }

    return;
}

if (args is not [{ } fileName])
{
    await Console.Error.WriteLineAsync("Usage: LoxInterpreter <filename>");
    Environment.Exit(64);   // Usage error
    return;
}

FileInfo file   = new(fileName);

if (!file.Exists)
{
    await Console.Error.WriteLineAsync($"File {file.FullName} does not exist");
    Environment.Exit(66);   // Input error
    return;
}

if (file.Extension is not ".lox")
{
    await Console.Error.WriteLineAsync($"File {file.FullName} is not a recognized Lox file (invalid extension)");
    Environment.Exit(66);   // Input error
    return;
}

string source = await file.OpenText().ReadToEndAsync();
LoxScanner scanner = new(source);

ReadOnlyCollection<Token> tokens = await scanner.TokenizeAsync();

LoxParser parser = new(tokens);
ReadOnlyCollection<LoxStatement> program = await parser.ParseAsync();

LoxInterpreter interpreter = new();
await interpreter.InterpretAsync(program);

if (LoxErrorUtils.HadParsingError)
{
    Environment.Exit(65);   // Data error
}
else if (LoxErrorUtils.HadRuntimeError)
{
    Environment.Exit(70);   // Software error
}