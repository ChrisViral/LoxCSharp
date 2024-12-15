using System.Runtime.CompilerServices;
using FastEnumUtility;
using Lox.Common;
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
    private enum Precedence : byte
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

    private void ParseExpression() => ParsePrecedence(Precedence.ASSIGNMENT);

    private void ParsePrecedence(Precedence precedence)
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

    private void ParseGrouping()
    {
        ParseExpression();
        EnsureNextToken(TokenType.RIGHT_PAREN, "Closing parentheses expected.");
    }

    private void ParseBinary()
    {
        Token operatorToken = this.previousToken;
        ParsePrecedence(Table.GetRule(operatorToken.Type).precedence + 1);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (operatorToken.Type)
        {
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

            default:
                throw new LoxParseException("Invalid binary operator: " + FastEnum.ToString<TokenType, TokenTypeBooster>(operatorToken.Type));
        }
    }

    private void ParseUnary()
    {
        Token operatorToken = this.previousToken;
        ParsePrecedence(Precedence.UNARY);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (operatorToken.Type)
        {
            case TokenType.MINUS:
                EmitOpcode(LoxOpcode.NEGATE, operatorToken.Line);
                break;

            default:
                throw new LoxParseException("Invalid unary operator: " + FastEnum.ToString<TokenType, TokenTypeBooster>(operatorToken.Type));
        }
    }

    private void ParseNumber()
    {
        if (!EmitConstant(double.Parse(this.previousToken.Lexeme)))
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
