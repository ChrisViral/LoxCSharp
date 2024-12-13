using Lox.Common;

namespace Lox.VM.Scanner;

public class LoxScanner : ILoxScanner<Token>
{
    /// <inheritdoc />
    public IEnumerable<Token> Tokenize(string source)
    {
        yield break;
    }
}
