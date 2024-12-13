using Lox.Common;
using Lox.VM.Scanner;

namespace Lox.VM;

public class LoxInterpreter : ILoxInterpreter<Token>
{

    /// <inheritdoc />
    public void Interpret(IEnumerable<Token> tokens) { }
}
