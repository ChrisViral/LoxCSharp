using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Syntax.Statements.Declarations;

/// <summary>
/// Lox declaration statement
/// </summary>
/// <param name="Identifier">Variable identifier</param>
public abstract record LoxDeclaration(in Token Identifier) : LoxStatement;
