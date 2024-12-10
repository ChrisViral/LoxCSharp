namespace Lox.Runtime.Types.Classes;

/// <summary>
/// Lox object instance
/// </summary>
/// <param name="definition">Object class definition</param>
public sealed class LoxInstance(LoxClass definition) : LoxObject
{
    public LoxClass Definition { get; } = definition;

    public override string ToString() => $"[instance {this.Definition.Identifier.Lexeme}]";
}
