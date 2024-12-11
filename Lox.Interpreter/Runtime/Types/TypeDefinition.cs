using Lox.Interpreter.Runtime.Functions;
using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Runtime.Types;

/// <summary>
/// Lox type definition
/// </summary>
/// <param name="identifier">Type identifier</param>
/// <param name="superclass">Type superclass</param>
/// <param name="methods">Type methods</param>
public class TypeDefinition(in Token identifier, LoxType? superclass, Dictionary<string, FunctionDefinition> methods)
    : LoxType(identifier, superclass, methods, superclass is not null ? TypeKind.SUBCLASS : TypeKind.CLASS);
