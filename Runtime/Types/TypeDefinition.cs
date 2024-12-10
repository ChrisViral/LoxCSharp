using Lox.Runtime.Functions;
using Lox.Scanner;

namespace Lox.Runtime.Types;

/// <summary>
/// Lox type definition
/// </summary>
/// <param name="identifier">Type identifier</param>
/// <param name="methods">Type methods</param>
public class TypeDefinition(in Token identifier, Dictionary<string, FunctionDefinition> methods) : LoxType(identifier, methods, TypeKind.CLASS);
