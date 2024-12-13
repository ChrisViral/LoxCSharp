using JetBrains.Annotations;
using Lox.Interpreter.Exceptions.Runtime;
using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Utils;

[PublicAPI]
public static class LoxErrorUtils
{
    /// <summary>
    /// Maximum amount of parameters allowed for function invokes
    /// </summary>
    public const int MAX_PARAMS = byte.MaxValue + 1;

    /// <summary>
    /// If the interpreter has encountered an error while parsing the Lox script
    /// </summary>
    public static bool HadParsingError { get; internal set; }

    /// <summary>
    /// If the interpreter has encountered an error while running the Lox script
    /// </summary>
    public static bool HadRuntimeError { get; internal set; }

    /// <summary>
    /// Reports an error at the specified line
    /// </summary>
    /// <param name="line">Error line</param>
    /// <param name="message">Error message</param>
    public static void ReportError(int line, string message) => ReportErrorInternal($"[line {line}] Error: {message}");

    /// <summary>
    /// Reports an error at the specified line
    /// </summary>
    /// <param name="line">Error line</param>
    /// <param name="location">Error location</param>
    /// <param name="message">Error message</param>
    public static void ReportError(int line, string location, string message) => ReportErrorInternal($"[line {line}] Error {location}: {message}");

    /// <summary>
    /// Reports an error at the specified token
    /// </summary>
    /// <param name="token">Error token</param>
    /// <param name="message">Error message</param>
    public static void ReportParseError(in Token token, string message) => ReportError(token.Line, token.IsEOF ? "at end" : $"at '{token.Lexeme}'", message);

    /// <summary>
    /// Reports a warning at the specified token
    /// </summary>
    /// <param name="token">Error token</param>
    /// <param name="message">Error message</param>
    public static void ReportParseWarning(in Token token, string message) => ReportWarningInternal($"[line {token.Line}] Warning {(token.IsEOF ? "at end" : $"at '{token.Lexeme}'")}: {message}");

    /// <summary>
    /// Prints out an error message and sets the error flag
    /// </summary>
    /// <param name="message">Error message</param>
    private static void ReportErrorInternal(string message)
    {
        HadParsingError = true;
        Console.Error.WriteLine(message);
    }

    /// <summary>
    /// Prints out an warning message
    /// </summary>
    /// <param name="message">Warning message</param>
    private static void ReportWarningInternal(string message) => Console.Error.WriteLine(message);

    /// <summary>
    /// Reports a specified <see cref="LoxRuntimeException"/>
    /// </summary>
    /// <param name="exception">Exception to report</param>
    public static void ReportRuntimeException(LoxRuntimeException exception)
    {
        HadRuntimeError = true;
        string message = $"{exception.Message}\n[line {exception.Token.Line}]";
        Console.Error.WriteLine(message);
    }
}
