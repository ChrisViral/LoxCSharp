using Lox.Common;
using Lox.VM.Scanner;

namespace Lox.VM;

public sealed class REPL : LoxREPL<Token, LoxScanner, LoxInterpreter>
{
    /// <inheritdoc />
    protected override void Evaluate(string line)
    {
        this.Interpreter.Interpret(line);
    }
}