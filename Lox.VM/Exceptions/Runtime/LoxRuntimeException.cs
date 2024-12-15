using JetBrains.Annotations;
using Lox.Common.Exceptions;
using Lox.VM.Scanner;

namespace Lox.VM.Exceptions.Runtime;

/// <summary>
/// Generalized Lox runtime exception
/// </summary>
[PublicAPI]
public class LoxRuntimeException : LoxException
{
    /// <summary>
    /// Error token
    /// </summary>
    public int Line { get; protected init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class.
    /// </summary>
    public LoxRuntimeException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class.
    /// </summary>
    /// <param name="line">Errored line</param>
    public LoxRuntimeException(int line) => this.Line = line;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LoxRuntimeException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="line">Errored line</param>
    public LoxRuntimeException(string? message, int line) : base(message) => this.Line = line;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public LoxRuntimeException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="line">Errored line</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public LoxRuntimeException(string? message, int line, Exception? innerException) : base(message, innerException) => this.Line = line;
}
