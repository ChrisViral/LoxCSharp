using CodeCrafters.Interpreter.Scanning;
using JetBrains.Annotations;

namespace CodeCrafters.Interpreter.Exceptions.Runtime;

/// <summary>
/// Lox invalid operand exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidOperandException : LoxRuntimeException
{
    /// <inheritdoc />
    public LoxInvalidOperandException() { }

    /// <inheritdoc />
    public LoxInvalidOperandException(Token token) : base(token) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, Token token) : base(message, token) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, Token token, Exception? innerException) : base(message, token, innerException) { }
}
