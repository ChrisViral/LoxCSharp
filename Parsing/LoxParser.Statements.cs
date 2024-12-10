using Lox.Exceptions;
using Lox.Runtime.Types.Functions;
using Lox.Scanning;
using Lox.Syntax.Expressions;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;
using Lox.Utils;

namespace Lox.Parsing;

public partial class LoxParser
{
    #region Statements
    /// <summary>
    /// Parses a declaration
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private LoxStatement? ParseDeclaration(in ReadOnlySpan<Token> tokens)
    {
        try
        {
            Token currentToken = MoveNextToken(tokens);
            switch (currentToken.Type)
            {
                case TokenType.VAR:
                    return ParseVariableDeclaration(tokens);

                case TokenType.FUN:
                    return ParseFunctionDeclaration(tokens, FunctionKind.FUNCTION);

                default:
                    this.currentIndex--;
                    return ParseStatement(tokens);
            }
        }
        catch (LoxParseException)
        {
            Synchronize(tokens);
            return null;
        }
    }

    /// <summary>
    /// Parses a variable declaration
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private VariableDeclaration ParseVariableDeclaration(in ReadOnlySpan<Token> tokens)
    {
        Token identifier = EnsureNextToken(tokens, TokenType.IDENTIFIER, "Expect variable name.");
        LoxExpression? initializer = null;
        if (TryMatchToken(tokens, TokenType.EQUAL, out _))
        {
            initializer = ParseExpression(tokens);
        }

        EnsureNextToken(tokens, TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        return new VariableDeclaration(identifier, initializer);
    }

    /// <summary>
    /// Parses a variable declaration
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <param name="kind">Function kind</param>
    /// <returns>The parsed statement</returns>
    private FunctionDeclaration ParseFunctionDeclaration(in ReadOnlySpan<Token> tokens, FunctionKind kind)
    {
        string kindName = EnumUtils.ToString(kind).ToLowerInvariant();
        Token identifier = EnsureNextToken(tokens, TokenType.IDENTIFIER, $"Expect {EnumUtils.ToString(kind).ToLowerInvariant()} name.");
        EnsureNextToken(tokens, TokenType.LEFT_PAREN, $"Expect '(' after {kindName} name.");
        List<Token> parameters;
        if (CheckCurrentToken(tokens, TokenType.RIGHT_PAREN))
        {
            parameters = [];
        }
        else
        {
            parameters = new List<Token>(4);
            do
            {
                if (parameters.Count is LoxErrorUtils.MAX_PARAMS)
                {
                    LoxErrorUtils.ReportParseError(CurrentToken(tokens), $"Can't have more than {LoxErrorUtils.MAX_PARAMS - 1} parameters.");
                }

                parameters.Add(EnsureNextToken(tokens, TokenType.IDENTIFIER, "Expect parameter name."));
            }
            while (TryMatchToken(tokens, TokenType.COMMA, out _));
        }

        EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after parameters");
        EnsureNextToken(tokens, TokenType.LEFT_BRACE, $"Expect '{{' before {kindName} body.");
        BlockStatement body = ParseBlockStatement(tokens);
        return new FunctionDeclaration(identifier, parameters.AsReadOnly(), body);
    }

    /// <summary>
    /// Parses a statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private LoxStatement ParseStatement(in ReadOnlySpan<Token> tokens)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        Token current = MoveNextToken(tokens);
        switch (current.Type)
        {
            case TokenType.PRINT:
                return ParsePrintStatement(tokens);

            case TokenType.RETURN:
                return ParseReturnStatement(tokens, current);

            case TokenType.IF:
                return ParseIfStatement(tokens);

            case TokenType.WHILE:
                return ParseWhileStatement(tokens);

            case TokenType.FOR:
                return ParseForStatement(tokens);

            case TokenType.LEFT_BRACE:
                return ParseBlockStatement(tokens);

            default:
                // We don't want to have consumed the token if we're parsing an expression
                this.currentIndex--;
                return ParseExpressionStatement(tokens);
        }
    }

    /// <summary>
    /// Parses a print statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private PrintStatement ParsePrintStatement(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseExpression(tokens);
        EnsureNextToken(tokens, TokenType.SEMICOLON, "Expect ';' after value.");
        return new PrintStatement(expression);
    }

    /// <summary>
    /// Parses a return statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <param name="keyword">Return keyword</param>
    /// <returns>The parsed statement</returns>
    private ReturnStatement ParseReturnStatement(in ReadOnlySpan<Token> tokens, in Token keyword)
    {
        LoxExpression? value = null;
        if (!TryMatchToken(tokens, TokenType.SEMICOLON, out _))
        {
            value = ParseExpression(tokens);
            EnsureNextToken(tokens, TokenType.SEMICOLON, "Expect ';' after return value.");
        }
        return new ReturnStatement(keyword, value);
    }

    /// <summary>
    /// Parses an if statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private IfStatement ParseIfStatement(in ReadOnlySpan<Token> tokens)
    {
        EnsureNextToken(tokens, TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        LoxExpression condition = ParseExpression(tokens);
        EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

        LoxStatement ifBranch = ParseStatement(tokens);
        LoxStatement? elseBranch = null;
        if (TryMatchToken(tokens, TokenType.ELSE, out Token _))
        {
            elseBranch = ParseStatement(tokens);
        }

        return new IfStatement(condition, ifBranch, elseBranch);
    }

    /// <summary>
    /// Parses a while statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private WhileStatement ParseWhileStatement(in ReadOnlySpan<Token> tokens)
    {
        EnsureNextToken(tokens, TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
        LoxExpression condition = ParseExpression(tokens);
        EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after while condition.");

        LoxStatement bodyStatement = ParseStatement(tokens);
        return new WhileStatement(condition, bodyStatement);
    }

    /// <summary>
    /// Parses a while statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private ForStatement ParseForStatement(in ReadOnlySpan<Token> tokens)
    {
        // Ensure opening parenthesis
        EnsureNextToken(tokens, TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

        // Get initializer
        LoxStatement? initializer;
        Token currentToken = MoveNextToken(tokens);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (currentToken.Type)
        {
            case TokenType.VAR:
                initializer = ParseVariableDeclaration(tokens);
                break;

            case TokenType.SEMICOLON:
                initializer = null;
                break;

            default:
                this.currentIndex--;
                initializer = ParseExpressionStatement(tokens);
                break;
        }

        // Get condition
        LoxExpression? condition = null;
        if (!TryMatchToken(tokens, TokenType.SEMICOLON, out _))
        {
            condition = ParseExpression(tokens);
            EnsureNextToken(tokens, TokenType.SEMICOLON, "Expect ';' after loop condition.");
        }

        // Get increment
        ExpressionStatement? increment = null;
        if (!TryMatchToken(tokens, TokenType.RIGHT_PAREN, out _))
        {
            LoxExpression expression = ParseExpression(tokens);
            EnsureNextToken(tokens, TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");
            increment = new ExpressionStatement(expression);
        }

        // Get body
        LoxStatement bodyStatement = ParseStatement(tokens);
        return new ForStatement(initializer, condition, increment, bodyStatement);
    }

    /// <summary>
    /// Parses a block statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private BlockStatement ParseBlockStatement(in ReadOnlySpan<Token> tokens)
    {
        List<LoxStatement> blockStatements = new(4);
        while (!CheckEOF(tokens) && !CheckCurrentToken(tokens, TokenType.RIGHT_BRACE))
        {
            LoxStatement? statement = ParseDeclaration(tokens);
            if (statement is not null)
            {
                blockStatements.Add(statement);
            }
        }

        EnsureNextToken(tokens, TokenType.RIGHT_BRACE, "Expect '}' after block.");
        return new BlockStatement(blockStatements.AsReadOnly());
    }

    /// <summary>
    /// Parses an expression statement
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The parsed statement</returns>
    private ExpressionStatement ParseExpressionStatement(in ReadOnlySpan<Token> tokens)
    {
        LoxExpression expression = ParseExpression(tokens);
        EnsureNextToken(tokens, TokenType.SEMICOLON, "Expect ';' after value.");
        return new ExpressionStatement(expression);
    }
    #endregion
}
