using Lox.Common;
using Lox.VM.Compiler;
using Lox.VM.Runtime;
using Lox.VM.Scanner;

namespace Lox.VM;

public class LoxInterpreter : ILoxInterpreter<Token>, IDisposable
{
    private readonly LoxCompiler compiler = new();
    private readonly VirtualMachine vm = new();

    ~LoxInterpreter() => Dispose();

    /// <summary>
    /// Interpretation result
    /// </summary>
    public InterpretResult Result { get; private set; }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Interprets the provided source code
    /// </summary>
    /// <param name="source">Source to interpret</param>
    public void Interpret(string source)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        if (!this.compiler.Compile(source, out Dictionary<string, LoxValue>? internedStrings))
        {
            this.Result = InterpretResult.COMPILE_ERROR;
            return;
        }

        this.Result = this.vm.Run(this.compiler.Chunk, internedStrings);
    }

    /// <inheritdoc />
    void ILoxInterpreter<Token>.Interpret(IEnumerable<Token> tokens) { }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        this.compiler.Dispose();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }
}
