using CodeCrafters.Interpreter.Exceptions;
using CodeCrafters.Interpreter.Exceptions.Runtime;
using CodeCrafters.Interpreter.Scanning;
using JetBrains.Annotations;

namespace CodeCrafters.Interpreter.Utils;

[PublicAPI]
public static class LoxErrorUtils
{
    /// <summary>
    /// Error buffer
    /// </summary>
    private static readonly Queue<string> ErrorBuffer = [];

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
    /// If error messages should be buffered or flushed automatically
    /// </summary>
    public static bool BufferErrors { get; internal set; }

    /// <summary>
    /// Clears the error buffer
    /// </summary>
    public static void ClearBuffer() => ErrorBuffer.Clear();

    /// <summary>
    /// Flushes and prints the error buffer
    /// </summary>
    public static void FlushBuffer()
    {
        while (ErrorBuffer.TryDequeue(out string? message))
        {
            Console.Error.WriteLine(message);
        }
    }

    /// <summary>
    /// Swaps the contents of the error buffer with another
    /// </summary>
    /// <param name="otherBuffer">Buffer to swap contents with</param>
    public static void SwapBuffers(Queue<string> otherBuffer)
    {
        int originalErrorCount = ErrorBuffer.Count;
        int originalOtherCount = otherBuffer.Count;

        for (int i = 0; i < originalErrorCount; i++)
        {
            otherBuffer.Enqueue(ErrorBuffer.Dequeue());
        }

        for (int i = 0; i < originalOtherCount; i++)
        {
            ErrorBuffer.Enqueue(otherBuffer.Dequeue());
        }
    }

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
    /// Prints out an error message and sets the error flag
    /// </summary>
    /// <param name="message">Error message</param>
    private static void ReportErrorInternal(string message)
    {
        HadParsingError = true;
        if (BufferErrors)
        {
            ErrorBuffer.Enqueue(message);
        }
        else
        {
            Console.Error.WriteLine(message);
        }
    }

    /// <summary>
    /// Reports a specified <see cref="LoxRuntimeException"/>
    /// </summary>
    /// <param name="exception">Exception to report</param>
    public static void ReportRuntimeException(LoxRuntimeException exception)
    {
        HadRuntimeError = true;
        string message = $"{exception.Message}\n[line {exception.Token.Line}]";
        if (BufferErrors)
        {
            ErrorBuffer.Enqueue(message);
        }
        else
        {
            Console.Error.WriteLine(message);
        }
    }
}
