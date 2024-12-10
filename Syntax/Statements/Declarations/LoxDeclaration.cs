using Lox.Scanning;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Lox declaration statement
/// </summary>
/// <param name="Identifier">Variable identifier</param>
public abstract record LoxDeclaration(in Token Identifier) : LoxStatement;
