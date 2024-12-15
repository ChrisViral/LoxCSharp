using JetBrains.Annotations;
using Lox.Common.Exceptions;

namespace Lox.VM.Exceptions;

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
