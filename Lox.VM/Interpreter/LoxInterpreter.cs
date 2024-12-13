using Lox.Common;
using Lox.VM.Scanner;

namespace Lox.VM;

public class LoxInterpreter : ILoxInterpreter<Token>
{
    /// <summary>
    /// Interpretation result
    /// </summary>
    public InterpretResult Result { get; private set; }

    /// <inheritdoc />
    public void Interpret(IEnumerable<Token> tokens) { }
}
