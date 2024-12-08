using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using CodeCrafters.Interpreter.Parsing;
using CodeCrafters.Interpreter.Runtime;
using CodeCrafters.Interpreter.Scanning;
using CodeCrafters.Interpreter.Syntax;
using CodeCrafters.Interpreter.Syntax.Expressions;
using CodeCrafters.Interpreter.Syntax.Statements;

namespace CodeCrafters.Interpreter.Utils;

/// <summary>
/// Lox REPL helper
/// </summary>
public sealed class REPL
{
    /// <summary>
    /// REPL operation mode
    /// </summary>
    public enum REPLMode
    {
        DEFAULT,
        TOKENIZE,
        SYNTAX,
        INTERPRET
    }

    #region Constants
    /// <summary>
    /// Prompt string
    /// </summary>
    private const string PROMPT = "> ";
    /// <summary>
    /// Exit command
    /// </summary>
    private const string EXIT = "exit";
    #endregion

    #region Fields
    private readonly Queue<string> tempErrorBuffer = [];
    #endregion

    #region Properties
    /// <summary>
    /// REPL mode
    /// </summary>
    public REPLMode Mode { get; }

    /// <summary>
    /// REPL scanner
    /// </summary>
    private LoxScanner Scanner { get; } = new();

    /// <summary>
    /// REPL parser
    /// </summary>
    private LoxParser Parser { get; } = new();

    /// <summary>
    /// REPL interpreter
    /// </summary>
    private LoxInterpreter Interpreter { get; } = new();

    /// <summary>
    /// REPL printer
    /// </summary>
    private AstPrinter Printer { get; } = new();
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new REPL under default mode
    /// </summary>
    public REPL() { }

    /// <summary>
    /// Creates a new REPL under the mode provided by the argument
    /// </summary>
    /// <param name="arg">Mode argument</param>
    public REPL(string arg) => this.Mode = arg switch
    {
        "tokenize" => REPLMode.TOKENIZE,
        "parse"    => REPLMode.SYNTAX,
        "evaluate" => REPLMode.INTERPRET,
        _          => REPLMode.DEFAULT
    };
    #endregion

    #region Methods
    /// <summary>
    /// Enters a REPL cycle
    /// </summary>
    public async Task BeginREPL()
    {
        LoxErrorUtils.BufferErrors = this.Mode is REPLMode.INTERPRET;

        // Print header
        Version version = Assembly.GetExecutingAssembly().GetName().Version!;
        await Console.Out.WriteLineAsync("Lox v" + version);

        // Begin prompt
        await Console.Out.WriteAsync(PROMPT);
        string? line = await Console.In.ReadLineAsync();
        while (line is not null and not EXIT)
        {
            try
            {
                await Evaluate(line);
            }
            catch
            {
                // Ignored
            }

            LoxErrorUtils.HadParsingError = false;
            LoxErrorUtils.HadRuntimeError = false;

            // Get next line
            await Console.Out.WriteAsync(PROMPT);
            line = await Console.In.ReadLineAsync();
        }

        LoxErrorUtils.BufferErrors = false;
    }

    /// <summary>
    /// Evaluates a code line on the proper current mode
    /// </summary>
    /// <param name="line">Code line</param>
    /// <returns>The proper evaluate task</returns>
    /// <exception cref="InvalidEnumArgumentException">If <see cref="Mode"/> is an invalid REPL operation mode</exception>
    private Task Evaluate(string line)
    {
        switch (this.Mode)
        {
            case REPLMode.TOKENIZE:
                return EvaluateTokens(line);

            case REPLMode.SYNTAX:
                return EvaluateAST(line);

            case REPLMode.INTERPRET:
                return EvaluateValue(line);

            case REPLMode.DEFAULT:
                goto case REPLMode.INTERPRET;

            default:
                throw new InvalidEnumArgumentException(nameof(this.Mode), (int)this.Mode, typeof(REPLMode));
        }
    }

    /// <summary>
    /// Evaluates a code line
    /// </summary>
    /// <param name="line">line to evaluate</param>
    private async Task EvaluateTokens(string line)
    {
        this.Scanner.Source = line;
        foreach (Token token in await this.Scanner.TokenizeAsync())
        {
            await Console.Out.WriteLineAsync(token.ToString());
        }
    }

    /// <summary>
    /// Evaluates a code line
    /// </summary>
    /// <param name="line">line to evaluate</param>
    private async Task EvaluateAST(string line)
    {
        this.Scanner.Source = line;
        ReadOnlyCollection<Token> tokens = await this.Scanner.TokenizeAsync();
        this.Parser.UpdateSourceTokens(tokens);

        ReadOnlyCollection<LoxStatement> program = await this.Parser.ParseAsync();

        foreach (LoxStatement statement in program)
        {
            this.Printer.Print(statement);
        }
    }

    /// <summary>
    /// Evaluates a code line
    /// </summary>
    /// <param name="line">line to evaluate</param>
    private async Task EvaluateValue(string line)
    {
        this.Scanner.Source = line;
        ReadOnlyCollection<Token> tokens = await this.Scanner.TokenizeAsync();
        this.Parser.UpdateSourceTokens(tokens);

        ReadOnlyCollection<LoxStatement> program = await this.Parser.ParseAsync();
        if (program.Count is not 0)
        {
            await this.Interpreter.InterpretAsync(program);
        }
        else
        {
            // Clear errors
            LoxErrorUtils.HadParsingError = false;
            LoxErrorUtils.SwapBuffers(this.tempErrorBuffer);

            this.Parser.Reset();
            LoxExpression? expression = await this.Parser.ParseExpressionAsync();
            if (expression is not null)
            {
                await this.Interpreter.InterpretAsync(expression);
            }
            else
            {
                LoxErrorUtils.SwapBuffers(this.tempErrorBuffer);
                LoxErrorUtils.FlushBuffer();
            }

            this.tempErrorBuffer.Clear();
        }

        if (LoxErrorUtils.HadRuntimeError)
        {
            LoxErrorUtils.FlushBuffer();
        }
    }
    #endregion
}
