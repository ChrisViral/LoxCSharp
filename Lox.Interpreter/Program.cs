#define DEBUG_TRACE

using System.Collections.ObjectModel;
using Lox.Interpreter;
using Lox.Interpreter.Scanner;
using Lox.Interpreter.Syntax.Statements;
using Lox.Interpreter.Utils;

if (args is [])
{

    REPL repl = new();
    try
    {
        await repl.BeginREPLAsync();
        Environment.Exit(0);
    }
    catch (Exception e)
    {
        await Console.Error.WriteLineAsync($"[{e.GetType().Name}]: {e.Message}\n{e.StackTrace}");
        Environment.Exit(70);    // Software error
    }
}

if (args is not [{ } fileName])
{
    await Console.Error.WriteLineAsync("Usage: LoxInterpreter <filename>");
    Environment.Exit(64);   // Usage error
    return;
}

FileInfo file = new(fileName);

if (!file.Exists)
{
    await Console.Error.WriteLineAsync($"File {file.FullName} does not exist");
    Environment.Exit(66);   // Input error
}

if (file.Extension is not ".lox")
{
    await Console.Error.WriteLineAsync($"File {file.FullName} is not a recognized Lox file (invalid extension)");
    Environment.Exit(66);   // Input error
}

string source = await file.OpenText().ReadToEndAsync();
LoxScanner scanner = new(source);

ReadOnlyCollection<Token> tokens = await scanner.TokenizeAsync();

LoxInterpreter interpreter = new();
interpreter.Parser.UpdateSourceTokens(tokens);
ReadOnlyCollection<LoxStatement> program = await interpreter.Parser.ParseAsync();
if (LoxErrorUtils.HadParsingError)
{
    Environment.Exit(65);   // Data error
}

await interpreter.Resolver.ResolveAsync(program);
if (LoxErrorUtils.HadParsingError)
{
    Environment.Exit(65);   // Data error
}

await interpreter.InterpretAsync(program);
if (LoxErrorUtils.HadRuntimeError)
{
    Environment.Exit(70);   // Software error
}