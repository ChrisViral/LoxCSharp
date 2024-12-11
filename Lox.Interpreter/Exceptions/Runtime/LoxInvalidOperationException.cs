using JetBrains.Annotations;
using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Exceptions.Runtime;

/// <summary>
/// Lox invalid operation exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidOperationException : LoxRuntimeException
{
    /// <inheritdoc />
    public LoxInvalidOperationException() { }

    /// <inheritdoc />
    public LoxInvalidOperationException(Token token) : base(token) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, Token token) : base(message, token) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, Token token, Exception? innerException) : base(message, token, innerException) { }
}