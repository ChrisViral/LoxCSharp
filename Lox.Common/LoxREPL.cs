using System.Reflection;
using JetBrains.Annotations;

namespace Lox.Common;

/// <summary>
/// Lox REPL helper
/// </summary>
[PublicAPI]
public class LoxREPL<TToken, TScanner, TInterpreter>
    where TToken : struct
    where TScanner : ILoxScanner<TToken>, new()
    where TInterpreter : ILoxInterpreter<TToken>, new()
{
    #region Constants
    /// <summary>
    /// Prompt string
    /// </summary>
    protected const string PROMPT = "> ";
    /// <summary>
    /// Exit command
    /// </summary>
    protected const string EXIT = "exit";
    #endregion

    #region Properties
    /// <summary>
    /// REPL scanner
    /// </summary>
    protected TScanner Scanner { get; init; } = new();

    /// <summary>
    /// REPL interpreter
    /// </summary>
    protected TInterpreter Interpreter { get; init; } = new();
    #endregion

    #region Methods
    /// <summary>
    /// Enters a REPL cycle
    /// </summary>
    public virtual async Task BeginREPLAsync()
    {
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
                Evaluate(line);
            }
            catch
            {
                // Ignored
            }

            AfterEvaluate();

            // Get next line
            await Console.Out.WriteAsync(PROMPT);
            line = await Console.In.ReadLineAsync();
        }
    }

    /// <summary>
    /// Enters a REPL cycle
    /// </summary>
    public virtual void BeginREPL()
    {
        // Print header
        Version version = Assembly.GetCallingAssembly().GetName().Version!;
        Console.WriteLine("Lox v" + version);

        // Begin prompt
        Console.Write(PROMPT);
        string? line = Console.ReadLine();
        while (line is not null and not EXIT)
        {
            try
            {
                Evaluate(line);
            }
            catch
            {
                // Ignored
            }

            AfterEvaluate();

            // Get next line
            Console.Write(PROMPT);
            line = Console.ReadLine();
        }
    }

    /// <summary>
    /// Post-line evaluate callback
    /// </summary>
    protected virtual void AfterEvaluate() { }

    /// <summary>
    /// Evaluates a code line
    /// </summary>
    /// <param name="line">line to evaluate</param>
    protected virtual void Evaluate(string line) => this.Interpreter.Interpret(this.Scanner.Tokenize(line));
    #endregion
}
