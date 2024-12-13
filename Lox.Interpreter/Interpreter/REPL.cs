using Lox.Common;
using Lox.Interpreter.Scanner;
using Lox.Interpreter.Utils;

namespace Lox.Interpreter;

/// <summary>
/// Lox REPL helper
/// </summary>
public sealed class REPL : LoxREPL<Token, LoxScanner, LoxInterpreter>
{
    #region Methods
    /// <inheritdoc />
    protected override void AfterEvaluate()
    {
        LoxErrorUtils.HadParsingError = false;
        LoxErrorUtils.HadRuntimeError = false;
    }
    #endregion
}
