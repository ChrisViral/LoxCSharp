using Lox.Scanning;

namespace Lox.Runtime.Types;

/// <summary>
/// Lox custom class object
/// </summary>
/// <param name="identifier">Object identifier</param>
public abstract class LoxObject(in Token identifier)
{
    /// <summary>
    /// Object identifier
    /// </summary>
    public Token Identifier { get; protected init; } = identifier;

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString() => "[obj]";
}
