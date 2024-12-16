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
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (this.currentToken.Type)
            {
                case TokenType.VAR:
                    ParseVariableDeclaration();
                    break;

                default:
                    ParseStatement();
                    break;
            }
        }
        catch (LoxParseException)
        {
            Synchronize();
        }
    }

    private void ParseVariableDeclaration()
    {
        MoveNextToken();
        Token identifier = EnsureNextToken(TokenType.IDENTIFIER, "Expected variable name.");
        if (TryMatchToken(TokenType.EQUAL, out Token _))
        {
            ParseExpression();
            EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            EmitStringConstant(identifier.Lexeme, ConstantType.DEF_GLOBAL);
        }
        else
        {
            EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            EmitStringConstant(identifier.Lexeme, ConstantType.NDF_GLOBAL);
        }
    }

    /// <summary>
    /// Parses a statement
    /// </summary>
    private void ParseStatement()
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
