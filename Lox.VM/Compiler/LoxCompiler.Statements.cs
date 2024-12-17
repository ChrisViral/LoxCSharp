using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Parses a variable declaration
    /// </summary>
    private void ParseVariableDeclaration()
    {
        MoveNextToken();
        Token identifier = EnsureNextToken(TokenType.IDENTIFIER, "Expected variable name.");

        if (this.IsGlobalScope)
        {
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
        else
        {
            DeclareLocal();
            if (TryMatchToken(TokenType.EQUAL, out Token _))
            {
                ParseExpression();
            }
            else
            {
                EmitOpcode(LoxOpcode.NIL);
            }
            EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
        }
    }

    /// <summary>
    /// Declares a new local variable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DeclareLocal()
    {
        if (this.locals.Count is ushort.MaxValue) ReportCompileError(this.previousToken, $"Local variable limit ({ushort.MaxValue}) exceeded.");

        for (int i = this.locals.Count - 1; i >= 0; i--)
        {
            Local local = this.locals[i];
            if (local.Depth is not -1 && local.Depth < this.scopeDepth) break;
            if (local.Identifier.Lexeme == this.previousToken.Lexeme)
            {
                ReportCompileError(this.previousToken, "Variable with same name already declared in this scope.");
            }
        }

        this.locals.Add(new Local(this.previousToken, this.scopeDepth));
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

            case TokenType.LEFT_BRACE:
                ParseBlockStatement();
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
    /// Parses a block statement
    /// </summary>
    private void ParseBlockStatement()
    {
        using Scope _ = Scope.Open(this);
        MoveNextToken();
        while (!this.IsEOF && !CheckCurrentToken(TokenType.RIGHT_BRACE))
        {
            ParseDeclaration();
        }

        EnsureNextToken(TokenType.RIGHT_BRACE, "Expected '}' after block.");
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
