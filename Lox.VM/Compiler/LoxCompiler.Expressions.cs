using System.Runtime.CompilerServices;
using FastEnumUtility;
using Lox.Common;
using Lox.Common.Exceptions;
using Lox.VM.Bytecode;
using Lox.VM.Scanner;

namespace Lox.VM.Compiler;

public partial class LoxCompiler
{
    /// <summary>
    /// Lox operator precendence
    /// </summary>
    private enum Precedence
    {
        NONE,       // Undefined
        ASSIGNMENT, // =
        OR,         // or
        AND,        // and
        EQUALITY,   // == !=
        COMPARISON, // < > <= >=
        TERM,       // + -
        FACTOR,     // * /
        UNARY,      // ! -
        CALL,       // . ()
        PRIMARY     // Literals & Identifiers
    }

    /// <summary>
    /// Parses an expression
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseExpression() => ParseWithPrecedence(Precedence.ASSIGNMENT);

    /// <summary>
    /// Parses the next expression of the given or lower precedence
    /// </summary>
    /// <param name="precedence">Expression precedence to parse</param>
    private void ParseWithPrecedence(Precedence precedence)
    {
        MoveNextToken();
        ParseFunc? prefix = Table.GetRule(this.previousToken.Type).Prefix;
        if (prefix is null)
        {
            ReportCompileError(this.previousToken, "Expected expression.");
            return;
        }

        bool canAssign = precedence <= Precedence.ASSIGNMENT;
        prefix(this, canAssign);
        for (ref ParseRule currentRule = ref Table.GetRule(this.currentToken.Type); precedence <= currentRule.Precedence; currentRule = ref Table.GetRule(this.currentToken.Type))
        {
            MoveNextToken();
            currentRule.Infix!(this, canAssign);
        }

        if (canAssign && TryMatchToken(TokenType.EQUAL, out Token equal))
        {
            ReportCompileError(equal, "Invalid assignment target");
        }
    }

    /// <summary>
    /// Parses a grouping expression
    /// </summary>
    private void ParseGrouping()
    {
        ParseExpression();
        EnsureNextToken(TokenType.RIGHT_PAREN, "Closing parentheses expected.");
    }

    /// <summary>
    /// Parses a binary expression
    /// </summary>
    /// <exception cref="LoxParseException"></exception>
    private void ParseBinary()
    {
        Token operatorToken = this.previousToken;
        ParseWithPrecedence(Table.GetRule(operatorToken.Type).Precedence + 1);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (operatorToken.Type)
        {
            // Mathematical operators
            case TokenType.PLUS:
                EmitOpcode(LoxOpcode.ADD, operatorToken.Line);
                break;
            case TokenType.MINUS:
                EmitOpcode(LoxOpcode.SUBTRACT, operatorToken.Line);
                break;
            case TokenType.STAR:
                EmitOpcode(LoxOpcode.MULTIPLY, operatorToken.Line);
                break;
            case TokenType.SLASH:
                EmitOpcode(LoxOpcode.DIVIDE, operatorToken.Line);
                break;

            // Logical operators
            case TokenType.EQUAL_EQUAL:
                EmitOpcode(LoxOpcode.EQUALS, operatorToken.Line);
                break;
            case TokenType.BANG_EQUAL:
                EmitOpcode(LoxOpcode.NOT_EQUALS, operatorToken.Line);
                break;
            case TokenType.GREATER:
                EmitOpcode(LoxOpcode.GREATER, operatorToken.Line);
                break;
            case TokenType.GREATER_EQUAL:
                EmitOpcode(LoxOpcode.GREATER_EQUALS, operatorToken.Line);
                break;
            case TokenType.LESS:
                EmitOpcode(LoxOpcode.LESS, operatorToken.Line);
                break;
            case TokenType.LESS_EQUAL:
                EmitOpcode(LoxOpcode.LESS_EQUALS, operatorToken.Line);
                break;

            default:
                throw new LoxParseException("Invalid binary operator: " + FastEnum.ToString<TokenType, TokenTypeBooster>(operatorToken.Type));
        }
    }

    /// <summary>
    /// Parses a unary expression
    /// </summary>
    /// <exception cref="LoxParseException"></exception>
    private void ParseUnary()
    {
        Token operatorToken = this.previousToken;
        if (operatorToken.Type is TokenType.MINUS && this.currentToken.Type is TokenType.NUMBER)
        {
            MoveNextToken();
            ParseNumber(true);
            return;
        }

        ParseWithPrecedence(Precedence.UNARY);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (operatorToken.Type)
        {
            case TokenType.BANG:
                EmitOpcode(LoxOpcode.NOT, operatorToken.Line);
                break;

            case TokenType.MINUS:
                EmitOpcode(LoxOpcode.NEGATE, operatorToken.Line);
                break;

            default:
                throw new LoxParseException("Invalid unary operator: " + FastEnum.ToString<TokenType, TokenTypeBooster>(operatorToken.Type));
        }
    }

    /// <summary>
    /// Parses an and operator expression
    /// </summary>
    private void ParseAnd()
    {
        Token andToken = this.previousToken;
        int andJumpAddress = EmitJump(LoxOpcode.JUMP_FALSE);
        ParseWithPrecedence(Precedence.AND);
        PatchJump(andToken, andJumpAddress);
    }

    /// <summary>
    /// Parses and or operator expression
    /// </summary>
    private void ParseOr()
    {
        Token orToken = this.previousToken;
        int orJumpAddress = EmitJump(LoxOpcode.JUMP_TRUE);
        ParseWithPrecedence(Precedence.OR);
        PatchJump(orToken, orJumpAddress);
    }

    /// <summary>
    /// Parses a number literal expression
    /// </summary>
    /// <param name="negate">If the number should be negated</param>
    private void ParseNumber(bool negate = false)
    {
        double value = double.Parse(this.previousToken.Lexeme);
        switch (value)
        {
            case 0d:
                EmitOpcode(LoxOpcode.ZERO);
                break;

            case 1d when !negate:
                EmitOpcode(LoxOpcode.ONE);
                break;

            default:
                EmitConstant(negate ? -value : value, out ushort _, ConstantType.CONSTANT);
                break;
        }
    }

    /// <summary>
    /// Parses a string literal expression
    /// </summary>
    private void ParseString() => EmitStringConstant(this.previousToken.Lexeme.AsSpan(1..^1), ConstantType.CONSTANT);

    /// <summary>
    /// Parses an identifier expression
    /// </summary>
    private void ParseIdentifier(bool canAssign)
    {
        if (TryResolveLocal(this.previousToken, out ushort index))
        {
            if (canAssign && TryMatchToken(TokenType.EQUAL, out Token _))
            {
                ParseExpression();
                EmitOpcode(LoxOpcode.SET_LOCAL, index);
            }
            else
            {
                EmitOpcode(LoxOpcode.GET_LOCAL, index);
            }
        }
        else
        {
            Token identifier = this.previousToken;
            if (canAssign && TryMatchToken(TokenType.EQUAL, out Token _))
            {
                ParseExpression();
                EmitStringConstant(identifier.Lexeme, ConstantType.SET_GLOBAL);
            }
            else
            {
                EmitStringConstant(identifier.Lexeme, ConstantType.GET_GLOBAL);
            }
        }
    }

    /// <summary>
    /// Tries to resolve the index of a local variable
    /// </summary>
    /// <param name="identifier">Variable identifier</param>
    /// <param name="index">Resulting index, if found</param>
    /// <returns><see langword="true"/> if a local with the given name was found, otherwise <see langword="false"/></returns>
    private bool TryResolveLocal(in Token identifier, out ushort index)
    {
        // Look through all current scopes starting at the end
        for (int i = this.scopeDepth - 1; i >= 0; i--)
        {
            Dictionary<string, Local> scope = this.localsPerScope[i];
            // ReSharper disable once InvertIf
            if (scope.TryGetValue(identifier.Lexeme, out Local local))
            {
                if (local.State is State.UNDEFINED)
                {
                    ReportCompileError(identifier, "Cannot read local variable in its own initializer.");
                }

                index = (ushort)local.Index;
                return true;
            }
        }
        index = 0;
        return false;
    }
}
