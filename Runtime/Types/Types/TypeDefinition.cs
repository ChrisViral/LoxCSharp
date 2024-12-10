﻿using Lox.Runtime.Types.Functions;
using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox type definition
/// </summary>
/// <param name="identifier">Type identifier</param>
/// <param name="methods">Type methods</param>
public class TypeDefinition(in Token identifier, Dictionary<string, FunctionDefinition> methods) : LoxType(identifier, methods);
