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
            if (TryMatchToken(TokenType.EQUAL, out Token _))
            {
                Local declared = DeclareLocal(identifier);
                ParseExpression();
                InitializeLocal(declared);
                EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            }
            else
            {
                DeclareLocal(identifier, State.DEFINED);
                EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
                EmitOpcode(LoxOpcode.NIL);
            }
        }
    }

    /// <summary>
    /// Declares a new local variable
    /// </summary>
    private Local DeclareLocal(in Token identifier, State state = State.UNDEFINED)
    {
        if (this.totalLocalsCount >= ushort.MaxValue) ReportCompileError(identifier, $"Local variable limit ({ushort.MaxValue}) exceeded.");

        Dictionary<string, Local> scope = this.localsPerScope[this.scopeDepth - 1];
        if (scope.ContainsKey(identifier.Lexeme))
        {
            ReportCompileError(identifier, "Variable with same name already declared in this scope.");
        }

        Local declared = new(identifier, this.totalLocalsCount++, state);
        scope.Add(identifier.Lexeme, declared);
        return declared;
    }

    /// <summary>
    /// Initializes the given last local variable
    /// </summary>
    /// <param name="template">Local template</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeLocal(in Local template)
    {
        this.localsPerScope[this.scopeDepth - 1][template.Identifier.Lexeme] = template with { State = State.DEFINED };
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

            case TokenType.IF:
                ParseIfStatement();
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
    /// Parses an if statement
    /// </summary>
    private void ParseIfStatement()
    {
        Token ifToken = MoveNextToken();
        EnsureNextToken(TokenType.LEFT_PAREN, "Expected '(' after 'if'.");
        ParseExpression();
        EnsureNextToken(TokenType.RIGHT_PAREN, "Expected ')' after condition.");

        int address = EmitJump(LoxOpcode.JMP_FALSE);
        ParseStatement();
        PatchJump(ifToken, address);
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
