using JetBrains.Annotations;
using Lox.Common.Exceptions;
using Lox.VM.Exceptions.Runtime;

namespace Lox.VM.Exceptions;

/// <summary>
/// Lox unknown opcode exception
/// </summary>
[PublicAPI]
public sealed class LoxUnknownOpcodeException : LoxRuntimeException
{
    /// <inheritdoc />
    public LoxUnknownOpcodeException() { }

    /// <inheritdoc />
    public LoxUnknownOpcodeException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxUnknownOpcodeException(string? message, Exception? innerException) : base(message, innerException) { }
}

