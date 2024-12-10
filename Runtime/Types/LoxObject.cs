using Lox.Scanning;

namespace Lox.Runtime.Types;

/// <summary>
/// Lox custom class object
/// </summary>
public abstract class LoxObject
{
    /// <summary>
    /// Lox object representation
    /// </summary>
    /// <returns>The representation of this Lox object</returns>
    public override string ToString() => "[obj]";
}
