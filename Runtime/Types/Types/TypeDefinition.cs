using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox type definition
/// </summary>
/// <param name="identifier">Object identifier</param>
public class TypeDefinition(in Token identifier) : LoxType(identifier);
