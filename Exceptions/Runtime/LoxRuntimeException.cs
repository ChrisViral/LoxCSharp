using JetBrains.Annotations;
using Lox.Scanning;

namespace Lox.Exceptions.Runtime;

/// <summary>
/// Generalized Lox runtime exception
/// </summary>
[PublicAPI]
public class LoxRuntimeException : LoxException
{
    /// <summary>
    /// Error token
    /// </summary>
    public Token Token { get; protected init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class.
    /// </summary>
    public LoxRuntimeException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class.
    /// </summary>
    /// <param name="token">Errored operand token</param>
    public LoxRuntimeException(Token token) => this.Token = token;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LoxRuntimeException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoxRuntimeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="token">Errored operand token</param>
    public LoxRuntimeException(string? message, Token token) : base(message) => this.Token = token;

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
    /// <param name="token">Errored operand token</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public LoxRuntimeException(string? message, Token token, Exception? innerException) : base(message, innerException) => this.Token = token;
}
