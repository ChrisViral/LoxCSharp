using Lox.Common;
using Lox.Common.Exceptions;
using Lox.VM.Bytecode;
using Lox.VM.Scanner;

namespace Lox.VM.Compiler;

public partial class LoxCompiler
{
    /// <summary>
    /// Parses a declaration
    /// </summary>
    private void ParseDeclaration()
    {
        try
        {
            ParseStatement();
        }
        catch (LoxParseException)
        {
            Synchronize();
        }
    }

    /// <summary>
    /// Parses a statement
    /// </summary>
    private void ParseStatement()
    {
        switch (this.currentToken.Type)
        {
            case TokenType.PRINT:
                ParsePrintStatement();
                break;

            default:
                ParseExpressionStatement();
                break;
        }
    }

    /// <summary>
    /// Parses a print statement
    /// </summary>
    private void ParsePrintStatement()
    {
        Token printToken = MoveNextToken();
        ParseExpression();
        EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after value.");
        EmitOpcode(LoxOpcode.PRINT, printToken.Line);
    }

    /// <summary>
    /// Parses an expression statement
    /// </summary>
    private void ParseExpressionStatement()
    {
        ParseExpression();
        EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after expression.");
        EmitOpcode(LoxOpcode.POP);
    }
}
