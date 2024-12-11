using System.Collections.ObjectModel;
using System.ComponentModel;
using Lox.Exceptions;
using Lox.Scanner;
using Lox.Syntax.Expressions;
using Lox.Utils;

namespace Lox;

public partial class LoxParser
{
    #region Expressions
    /// <summary>
    /// Parses an expression
    /// </summary>
    /// <returns>The parsed expression</returns>
    public async Task<LoxExpression?> ParseExpressionAsync() => await Task.Run(ParseExpression);

    /// <summary>
    /// Parses an expression
    /// </summary>
    /// <returns>The parsed expression</returns>
    public LoxExpression? ParseExpression()
    {
        try
        {
            ReadOnlySpan<Token> tokens = this.sourceTokens;
            return ParseExpression(tokens);
        }
        catch (LoxParseException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses an expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseExpression(in ReadOnlySpan<Token> tokens) => ParseAssignmentExpression(tokens);

    /// <summary>
    /// Parses an assignment expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseAssignmentExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseOrExpression(tokens);
        // ReSharper disable once InvertIf
        if (TryMatchToken(tokens, TokenType.EQUAL, out Token equals))
        {
            LoxExpression value = ParseAssignmentExpression(tokens);
            switch (expression)
            {
                case VariableExpression variable:
                    return new AssignmentExpression(variable.Identifier, value);

                case AccessExpression access:
                    return new SetExpression(access.Target, access.Identifier, value);

                default:
                    LoxErrorUtils.ReportParseError(equals, "Invalid assignment target.");
                    break;
            }
        }
        return expression;
    }

    /// <summary>
    /// Parses an or expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseOrExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseAndExpression(tokens);
        while (TryMatchToken(tokens, TokenType.OR, out Token operatorToken))
        {
            LoxExpression rightExpression = ParseAndExpression(tokens);
            expression = new LogicalExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses an and expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseAndExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseEqualityExpression(tokens);
        while (TryMatchToken(tokens, TokenType.AND, out Token operatorToken))
        {
            LoxExpression rightExpression = ParseEqualityExpression(tokens);
            expression = new LogicalExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses an equality expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseEqualityExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseComparisonExpression(tokens);
        while (TryMatchToken(tokens, [TokenType.EQUAL_EQUAL, TokenType.BANG_EQUAL], out Token operatorToken))
        {
            LoxExpression rightExpression = ParseComparisonExpression(tokens);
            expression = new BinaryExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses a comparison expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseComparisonExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseTermExpression(tokens);
        while (TryMatchToken(tokens, [TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL], out Token operatorToken))
        {
            LoxExpression rightExpression = ParseTermExpression(tokens);
            expression = new BinaryExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses a term arithmetic expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseTermExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseFactorExpression(tokens);
        while (TryMatchToken(tokens, [TokenType.PLUS, TokenType.MINUS], out Token operatorToken))
        {
            LoxExpression rightExpression = ParseFactorExpression(tokens);
            expression = new BinaryExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses a factor arithmetic expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseFactorExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseUnaryExpression(tokens);
        while (TryMatchToken(tokens, [TokenType.STAR, TokenType.SLASH], out Token operatorToken))
        {
            LoxExpression rightExpression = ParseUnaryExpression(tokens);
            expression = new BinaryExpression(expression, operatorToken, rightExpression);
        }
        return expression;
    }

    /// <summary>
    /// Parses a unary expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseUnaryExpression(in ReadOnlySpan<Token> tokens)
    {
        return TryMatchToken(tokens, [TokenType.BANG, TokenType.MINUS], out Token operatorToken)
                   ? new UnaryExpression(operatorToken, ParseUnaryExpression(tokens))
                   : ParseInvokeExpression(tokens);
    }

    /// <summary>
    /// Parses an invocation expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    private LoxExpression ParseInvokeExpression(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParsePrimaryExpression(tokens);
        while (TryMatchToken(tokens, [TokenType.LEFT_PAREN, TokenType.DOT], out Token token))
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (token.Type)
            {
                case TokenType.LEFT_PAREN:
                    ReadOnlyCollection<LoxExpression> parameters = ParseInvokeExpressionParameters(tokens);
                    Token terminator = EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
                    expression = new InvokeExpression(expression, parameters, terminator);
                    break;

                case TokenType.DOT:
                    Token identifier = EnsureNextToken(tokens, TokenType.IDENTIFIER, "Expect property name after '.'.");
                    expression = new AccessExpression(expression, identifier);
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(token.Type), (int)token.Type, typeof(TokenType));
            }
        }
        return expression;
    }

    /// <summary>
    /// Parses the argument list of an invocation expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed argument list</returns>
    private ReadOnlyCollection<LoxExpression> ParseInvokeExpressionParameters(in ReadOnlySpan<Token> tokens)
    {
        // Return early if there's no parameters
        if (CheckEOF(tokens) || CheckCurrentToken(tokens, TokenType.RIGHT_PAREN)) return new ReadOnlyCollection<LoxExpression>(Array.Empty<LoxExpression>());

        List<LoxExpression> parameters = new(4);
        do
        {
            if (parameters.Count is LoxErrorUtils.MAX_PARAMS)
            {
                LoxErrorUtils.ReportParseError(CurrentToken(tokens), $"Can't have more than {LoxErrorUtils.MAX_PARAMS - 1} arguments.");
            }

            parameters.Add(ParseExpression(tokens));
        }
        while (TryMatchToken(tokens, TokenType.COMMA, out _));
        parameters.TrimExcess();
        return parameters.AsReadOnly();
    }

    /// <summary>
    /// Parses a base primary expression
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed expression</returns>
    /// <exception cref="LoxParseException">If an invalid state is encountered</exception>
    private LoxExpression ParsePrimaryExpression(in ReadOnlySpan<Token> tokens)
    {
        Token currentToken = MoveNextToken(tokens);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (currentToken.Type)
        {
            case TokenType.NIL:
            case TokenType.TRUE:
            case TokenType.FALSE:
            case TokenType.STRING:
            case TokenType.NUMBER:
                if (currentToken.Literal.IsInvalid) ProduceException(currentToken, $"Literal token has no value ({currentToken})");
                return new LiteralExpression(currentToken.Literal);

            case TokenType.THIS:
                return new ThisExpression(currentToken);

            case TokenType.SUPER:
                EnsureNextToken(tokens, TokenType.DOT, "Expect '.' after 'super'.");
                Token methodIdentifier = EnsureNextToken(tokens, TokenType.IDENTIFIER, "Expect superclass method name.");
                return new SuperExpression(currentToken, methodIdentifier);

            case TokenType.IDENTIFIER:
                return new VariableExpression(currentToken);

            case TokenType.LEFT_PAREN:
                LoxExpression innerExpression = ParseExpression(tokens);
                EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new GroupingExpression(innerExpression);

            default:
                ProduceException(currentToken, "Expect expression.");
                return null!;
        }
    }
    #endregion
}
