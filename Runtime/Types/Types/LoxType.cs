using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox type object
/// </summary>
/// <param name="identifier">Object identifier</param>
public abstract class LoxType(in Token identifier) : LoxInvokable(identifier)
{
    /// <inheritdoc />
    public override int Arity => 0;

    /// <inheritdoc />
    public override LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments) => new LoxInstance(this);

    /// <inheritdoc />
    public override string ToString() => $"[class {this.Identifier.Lexeme}]";
}
