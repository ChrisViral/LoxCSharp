using System.Runtime.CompilerServices;
using FastEnumUtility;
using Lox.Common;
using Lox.Common.Exceptions;
using Lox.VM.Bytecode;
using Lox.VM.Exceptions;
using Lox.VM.Runtime;
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
        ParseFunc? prefix = Table.GetRule(this.previousToken.Type).prefix;
        if (prefix is null)
        {
            ReportCompileError(this.previousToken, "Expected expression.");
            return;
        }

        prefix(this);
        for (ref ParseRule currentRule = ref Table.GetRule(this.currentToken.Type); precedence <= currentRule.precedence; currentRule = ref Table.GetRule(this.currentToken.Type))
        {
            MoveNextToken();
            currentRule.infix!(this);
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
        ParseWithPrecedence(Table.GetRule(operatorToken.Type).precedence + 1);

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
    /// Parses a number literal expression
    /// </summary>
    /// <param name="negate">If the number should be negated</param>
    private void ParseNumber(bool negate = false)
    {
        double value = double.Parse(this.previousToken.Lexeme);
        if (!EmitConstant(negate ? -value : value))
        {
            ReportCompileError(this.currentToken, $"Constant limit ({LoxChunk.MAX_CONSTANT}) exceeded.");
        }
    }

    /// <summary>
    /// Emits the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to emit</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitOpcode(LoxOpcode opcode) => this.Chunk.AddOpcode(opcode, this.previousToken.Line);

    /// <summary>
    /// Emits the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to emit</param>
    /// <param name="line">Line at which the opcode is emited from</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitOpcode(LoxOpcode opcode, int line) => this.Chunk.AddOpcode(opcode, line);

    /// <summary>
    /// Emits the given value to the chunk
    /// </summary>
    /// <param name="value">Value to emit</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EmitConstant(in LoxValue value) => this.Chunk.AddConstant(value, this.previousToken.Line);
}
