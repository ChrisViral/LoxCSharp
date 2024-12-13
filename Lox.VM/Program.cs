﻿using Lox.VM;
using Lox.VM.Scanner;

if (args is [])
{

    REPL repl = new();
    try
    {
        await repl.BeginREPL();
        Environment.Exit(0);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"[{e.GetType().Name}]: {e.Message}\n{e.StackTrace}");
        Environment.Exit(70);    // Software error
    }
}

if (args is not [{ } fileName])
{
    Console.Error.WriteLine("Usage: LoxVM <filename>");
    Environment.Exit(64);   // Usage error
    return;
}

FileInfo file = new(fileName);

if (!file.Exists)
{
    Console.Error.WriteLine($"File {file.FullName} does not exist");
    Environment.Exit(66);   // Input error
}

if (file.Extension is not ".lox")
{
    Console.Error.WriteLine($"File {file.FullName} is not a recognized Lox file (invalid extension)");
    Environment.Exit(66);   // Input error
}

string source;
try
{
    source = await file.OpenText().ReadToEndAsync();
}
catch (Exception e)
{
    Console.Error.WriteLine($"Could not read file {file.FullName}.");
    Console.Error.WriteLine(e);
    Environment.Exit(74); // IO Error
    return;
}

LoxScanner scanner = new();
using (scanner.OpenPinScope(source))
{
    while (scanner.ScanNextToken(out Token token))
    {
        Console.WriteLine(token);
    }
}

// LoxInterpreter interpreter = new();
// interpreter.Interpret(scanner);

// Environment.Exit((int)interpreter.Result);
