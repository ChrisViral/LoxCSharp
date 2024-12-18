﻿using System.Collections.ObjectModel;
using Lox.Common;
using Lox.Interpreter.Exceptions.Runtime;
using Lox.Interpreter.Runtime;
using Lox.Interpreter.Scanner;
using Lox.Interpreter.Syntax.Expressions;
using Lox.Interpreter.Syntax.Statements;
using Lox.Interpreter.Utils;

namespace Lox.Interpreter;

/// <summary>
/// Lox program interpreter
/// </summary>
public sealed partial class LoxInterpreter : ILoxInterpreter<Token>, IExpressionVisitor<LoxValue>, IStatementVisitor
{
    #region Fields
    /// <summary>
    /// Locals resolve dictionary
    /// </summary>
    private readonly Dictionary<LoxExpression, Index> locals = new(ExpressionReferenceComparer.Comparer);
    #endregion

    #region Properties
    /// <summary>
    /// Lox parser
    /// </summary>
    public LoxParser Parser { get; } = new();
    /// <summary>
    /// Lox resolver
    /// </summary>
    public LoxResolver Resolver { get; }
    /// <summary>
    /// Runtime environment
    /// </summary>
    private LoxEnvironment CurrentEnvironment { get; set; } = new();
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new Lox interpreter
    /// </summary>
    public LoxInterpreter() => this.Resolver = new LoxResolver(this);
    #endregion

    #region Interpreter
    /// <summary>
    /// Interprets and prints a given Lox program
    /// </summary>
    /// <param name="tokens">Tokens</param>
    public async Task InterpretAsync(IEnumerable<Token> tokens) => await Task.Run(() => Interpret(tokens));

    /// <inheritdoc />
    public void Interpret(IEnumerable<Token> tokens)
    {
        this.Parser.UpdateSourceTokens(tokens);
        ReadOnlyCollection<LoxStatement> program = this.Parser.Parse();
        if (LoxErrorUtils.HadParsingError) return;

        this.Resolver.Resolve(program);
        if (LoxErrorUtils.HadParsingError) return;

        Interpret(program);
    }

    /// <summary>
    /// Interprets and prints a given Lox program
    /// </summary>
    /// <param name="program">Program statements</param>
    public async Task InterpretAsync(ReadOnlyCollection<LoxStatement> program) => await Task.Run(() => Interpret(program));

    /// <summary>
    /// Interprets and prints a given Lox program
    /// </summary>
    /// <param name="program">Program statements</param>
    public void Interpret(ReadOnlyCollection<LoxStatement> program)
    {
        try
        {
            Execute(program);
        }
        catch (LoxRuntimeException e)
        {
            LoxErrorUtils.ReportRuntimeException(e);
        }
    }

    /// <summary>
    /// Interprets and prints a given Lox expression
    /// </summary>
    /// <param name="expression">Expression to interpret</param>
    public async Task InterpretAsync(LoxExpression expression) => await Task.Run(() => Interpret(expression));

    /// <summary>
    /// Interprets and prints a given Lox expression
    /// </summary>
    /// <param name="expression">Expression to interpret</param>
    public void Interpret(LoxExpression expression)
    {
        try
        {
            LoxValue result = Evaluate(expression);
            Console.Out.WriteLineAsync(result.ToString());
        }
        catch (LoxRuntimeException e)
        {
            LoxErrorUtils.ReportRuntimeException(e);
        }
    }

    /// <summary>
    /// Executes a collection of statements
    /// </summary>
    /// <param name="statements">Statements to execute</param>
    private void Execute(IReadOnlyCollection<LoxStatement> statements)
    {
        foreach (LoxStatement statement in statements)
        {
            Execute(statement);
        }
    }

    /// <summary>
    /// Executes the specified statements in the provided environment
    /// </summary>
    /// <param name="statements">Statements to execute</param>
    /// <param name="environment">Environment to execute in</param>
    internal void Execute(IReadOnlyCollection<LoxStatement> statements, LoxEnvironment environment)
    {
        // Save previous env and restore after execution
        LoxEnvironment previousEnv = this.CurrentEnvironment;
        try
        {
            this.CurrentEnvironment = environment;
            Execute(statements);
        }
        finally
        {
            this.CurrentEnvironment = previousEnv;
        }
    }

    /// <summary>
    /// Executes a Lox statement
    /// </summary>
    /// <param name="statement">Statement to execute</param>
    private void Execute(LoxStatement statement) => statement.Accept(this);

    /// <summary>
    /// Evaluates a given Lox expression
    /// </summary>
    /// <param name="loxExpression">Expression to evaluate</param>
    /// <returns>The result of the expression</returns>
    private LoxValue Evaluate(LoxExpression loxExpression) => loxExpression.Accept(this);

    /// <summary>
    /// Sets the resolve depth for a given expression
    /// </summary>
    /// <param name="expression">Expression to save the resolve depth for</param>
    /// <param name="depth">Resolve depth</param>
    internal void SetResolveDepth(LoxExpression expression, Index depth) => this.locals[expression] = depth;

    /// <summary>
    /// Resolves a variables value
    /// </summary>
    /// <param name="identifier">Variable identifier</param>
    /// <param name="expression">Variable expression</param>
    /// <returns>The resolved variable's value</returns>
    private LoxValue ResolveVariable(in Token identifier, LoxExpression expression) => this.locals.TryGetValue(expression, out Index depth)
                                                                                           ? this.CurrentEnvironment.GetVariableAt(identifier, depth)
                                                                                           : LoxEnvironment.GetGlobalVariable(identifier);
    #endregion

    #region Static methods
    /// <summary>
    /// Validates that the given operand is a number
    /// </summary>
    /// <param name="operatorToken">Operator token</param>
    /// <param name="operand">Operand value</param>
    /// <param name="message">Error message</param>
    /// <returns>The operand as a double</returns>
    /// <exception cref="LoxInvalidOperandException">If <paramref name="operand"/> is not of type a <see cref="double"/></exception>
    private static double ValidateNumber(in Token operatorToken, in LoxValue operand, string message)
    {
        return operand.TryGetNumber(out double result) ? result : throw new LoxInvalidOperandException(message, operatorToken);
    }
    #endregion
}
