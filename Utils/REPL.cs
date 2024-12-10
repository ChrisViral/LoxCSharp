using System.Collections.ObjectModel;
using System.Reflection;
using Lox.Parsing;
using Lox.Runtime;
using Lox.Scanning;
using Lox.Syntax;
using Lox.Syntax.Expressions;
using Lox.Syntax.Statements;

namespace Lox.Utils;

/// <summary>
/// Lox REPL helper
/// </summary>
public sealed class REPL
{
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
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new REPL under default mode
    /// </summary>
    public REPL() { }
    #endregion

    #region Methods
    /// <summary>
    /// Enters a REPL cycle
    /// </summary>
    public async Task BeginREPL()
    {
        LoxErrorUtils.BufferErrors = true;

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
    /// Evaluates a code line
    /// </summary>
    /// <param name="line">line to evaluate</param>
    private async Task Evaluate(string line)
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
