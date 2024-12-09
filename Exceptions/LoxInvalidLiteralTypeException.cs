using JetBrains.Annotations;

namespace Lox.Exceptions;

/// <summary>
/// Invalid literal type exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidLiteralTypeException : LoxException
{
    /// <inheritdoc />
    public LoxInvalidLiteralTypeException() { }

    /// <inheritdoc />
    public LoxInvalidLiteralTypeException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidLiteralTypeException(string? message, Exception? innerException) : base(message, innerException) { }
}
