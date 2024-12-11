using JetBrains.Annotations;

namespace Lox.Interpreter.Exceptions;

/// <summary>
/// Lox parser exception
/// </summary>
[PublicAPI]
public sealed class LoxParseException : LoxException
{
    /// <inheritdoc />
    public LoxParseException() { }

    /// <inheritdoc />
    public LoxParseException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxParseException(string? message, Exception? innerException) : base(message, innerException) { }
}
