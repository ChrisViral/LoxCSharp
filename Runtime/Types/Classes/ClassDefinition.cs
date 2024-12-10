using Lox.Scanning;

namespace Lox.Runtime.Types.Classes;

/// <summary>
/// Lox class definition
/// </summary>
/// <param name="identifier">Object identifier</param>
public class ClassDefinition(in Token identifier) : LoxClass(identifier)
{
    
}
