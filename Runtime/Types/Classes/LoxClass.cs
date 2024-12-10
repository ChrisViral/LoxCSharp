using Lox.Scanning;

namespace Lox.Runtime.Types.Classes;

/// <summary>
/// Lox class object
/// </summary>
/// <param name="identifier">Object identifier</param>
public abstract class LoxClass(in Token identifier) : LoxObject(identifier)
{
    public override string ToString() => $"[class {this.Identifier.Lexeme}]";
}
