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
            if (TryMatchToken(TokenType.EQUAL))
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
            if (TryMatchToken(TokenType.EQUAL))
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

            case TokenType.WHILE:
                ParseWhileStatement();
                break;

            case TokenType.FOR:
                ParseForStatement();
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
        EmitOpcode(LoxOpcode.PRINT, printToken);
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

        int skipIfJumpAddress = EmitJump(LoxOpcode.JUMP_FALSE_POP);
        ParseStatement();

        if (!TryMatchToken(TokenType.ELSE, out Token elseToken))
        {
            PatchJump(ifToken, skipIfJumpAddress);
            return;
        }

        int skipElseJumpAddress = EmitJump(LoxOpcode.JUMP);
        PatchJump(ifToken, skipIfJumpAddress);
        ParseStatement();
        PatchJump(elseToken, skipElseJumpAddress);
    }

    /// <summary>
    /// Parses a while statement
    /// </summary>
    private void ParseWhileStatement()
    {
        Token whileToken = MoveNextToken();
        int whileStart = this.Chunk.Count;
        EnsureNextToken(TokenType.LEFT_PAREN, "Expected '(' after 'while'.");
        ParseExpression();
        EnsureNextToken(TokenType.RIGHT_PAREN, "Expected ')' after condition.");

        int exitJumpAddress = EmitJump(LoxOpcode.JUMP_FALSE_POP);
        ParseStatement();
        EmitLoop(whileToken, whileStart);
        PatchJump(whileToken, exitJumpAddress);
    }

    /// <summary>
    /// Parses a for statement
    /// </summary>
    private void ParseForStatement()
    {
        using Scope _ = Scope.Open(this);
        Token forToken = MoveNextToken();
        EnsureNextToken(TokenType.LEFT_PAREN, "Expected '(' after 'while'.");

        // If the next token is a semicolon, there is no initializer
        if (!TryMatchToken(TokenType.SEMICOLON))
        {
            // If the next token is actually var, then we have a variable declaration
            if (CheckCurrentToken(TokenType.VAR))
            {
                ParseVariableDeclaration();
            }
            else
            {
                ParseExpressionStatement();
            }
        }

        int forStart = this.Chunk.Count;
        int forJumpAddress = -1;
        // If the next token is a semicolon, there is no condition clause
        if (!TryMatchToken(TokenType.SEMICOLON))
        {
            ParseExpression();
            EnsureNextToken(TokenType.SEMICOLON, "Expected ';' after 'for' condition.");
            forJumpAddress = EmitJump(LoxOpcode.JUMP_FALSE_POP);
        }

        // If the next token is a parenthesis, there is no increment clause
        if (TryMatchToken(TokenType.RIGHT_PAREN))
        {
            // If there is no increment, simply parse the loop body
            ParseStatement();
        }
        else
        {
            // Parse increment expression
            int incrementStart = this.Chunk.Count;
            ParseExpression();
            EnsureNextToken(TokenType.RIGHT_PAREN, "Expected ')' after 'for' increment.");

            // Fetch emitted bytecode
            Span<byte> increment = stackalloc byte[this.Chunk.Count - incrementStart];
            this.Chunk.RequestLastBytes(increment);

            // Parse loop body
            ParseStatement();

            // Add back increment
            this.Chunk.AppendBytes(increment);
            EmitOpcode(LoxOpcode.POP);
        }

        // Finish by emitting loop
        EmitLoop(forToken, forStart);

        // If there was a condition, patch it now
        if (forJumpAddress is not -1)
        {
            PatchJump(forToken, forJumpAddress);
        }
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
