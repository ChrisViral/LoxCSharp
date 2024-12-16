using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.VM.Bytecode;
using Lox.VM.Exceptions;
using Lox.VM.Exceptions.Runtime;
using Lox.VM.Runtime;

namespace Lox.VM;

/// <summary>
/// VM interpretation result
/// </summary>
public enum InterpretResult
{
    SUCCESS       = 0,
    COMPILE_ERROR = 65,
    RUNTIME_ERROR = 70
}

/// <summary>
/// Lox Virtual Machine
/// </summary>
[PublicAPI]
public sealed partial class VirtualMachine : IDisposable
{
    #region Fields
    private unsafe byte* bytecode;
    private unsafe byte* instructionPointer;
    private Stack stack = null!;
    private LoxChunk currentChunk = null!;
    private Dictionary<string, LoxValue> interned = null!;
    private readonly List<IntPtr> allocations = new(byte.MaxValue + 1);
    #endregion

    #region Properties
    /// <summary>
    /// If the VM is currently running
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// If this VirtualMachine has been disposed or not
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the current instruction index
    /// </summary>
    private unsafe int CurrentIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(this.instructionPointer - this.bytecode) - 1;
    }

    /// <summary>
    /// Gets the current instruction line
    /// </summary>
    private int CurrentLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.currentChunk.GetLine(this.CurrentIndex);
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Finalizer
    /// </summary>
    ~VirtualMachine() => FreeResources();
    #endregion

    #region Methods
    /// <summary>
    /// Starts this Lox VM interpreter
    /// </summary>
    /// <param name="chunk">Chunk to execute</param>
    /// <param name="internedStrings">Dictionary of interned strings from the compiler</param>
    /// <returns>Completion status of the interpreter</returns>
    /// <exception cref="InvalidOperationException">If the interpreter is already running</exception>
    public unsafe InterpretResult Run(LoxChunk chunk, Dictionary<string, LoxValue> internedStrings)
    {
        if (this.IsRunning) throw new InvalidOperationException("This VM is already running");
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        this.IsRunning    = true;
        this.currentChunk = chunk;
        this.interned     = internedStrings;
        try
        {
            this.stack = new Stack();
            ReadOnlySpan<byte> bytecodeSpan = chunk.AsSpan();
            this.bytecode           = (byte*)Marshal.AllocHGlobal(bytecodeSpan.Length);
            this.instructionPointer = this.bytecode;
            using (UnmanagedMemoryStream stream = new(this.bytecode, bytecodeSpan.Length, bytecodeSpan.Length, FileAccess.Write))
            {
                stream.Write(bytecodeSpan);
            }

            return Run();
        }
        catch (LoxRuntimeException e)
        {
            Console.Error.WriteLine($"[line {e.Line}] {e.Message}");
            return InterpretResult.RUNTIME_ERROR;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Unhandled exception: \n" + e);
            return InterpretResult.RUNTIME_ERROR;
        }
        finally
        {
            FreeResources();
            this.stack              = null!;
            this.currentChunk       = null!;
            this.interned           = null!;
            this.IsRunning          = false;
        }
    }

    /// <summary>
    /// Interpreter loop
    /// </summary>
    /// <returns></returns>
    /// <exception cref="LoxUnknownOpcodeException">When an unknown opcode is encountered</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private InterpretResult Run()
    {
        while (true)
        {
            #if DEBUG_TRACE
            unsafe
            {
                this.stack.PrintStack();
                Utils.BytecodeUtils.PrintInstruction(this.currentChunk, this.instructionPointer, (int)(this.instructionPointer - this.bytecode));
            }
            #endif

            LoxOpcode instruction = (LoxOpcode)ReadByte();
            switch (instruction)
            {
                case LoxOpcode.NOP:
                    break;

                // Constants
                case LoxOpcode.CONSTANT_8:
                    ReadConstant8();
                    break;
                case LoxOpcode.CONSTANT_16:
                    ReadConstant16();
                    break;
                case LoxOpcode.CONSTANT_24:
                    ReadConstant24();
                    break;

                // Literals
                case LoxOpcode.NIL:
                    this.stack.Push(LoxValue.Nil);
                    break;
                case LoxOpcode.TRUE:
                    this.stack.Push(LoxValue.True);
                    break;
                case LoxOpcode.FALSE:
                    this.stack.Push(LoxValue.False);
                    break;

                // Unary operations
                case LoxOpcode.NOT:
                    this.stack.Push(this.stack.Pop().IsFalsey);
                    break;
                case LoxOpcode.NEGATE:
                    Negate();
                    break;

                // Binary mathematical operations
                case LoxOpcode.ADD:
                    Add();
                    break;
                case LoxOpcode.SUBTRACT:
                    Subtract();
                    break;
                case LoxOpcode.MULTIPLY:
                    Multiply();
                    break;
                case LoxOpcode.DIVIDE:
                    Divide();
                    break;

                // Binary logical operations
                case LoxOpcode.EQUALS:
                    Equal();
                    break;
                case LoxOpcode.NOT_EQUALS:
                    NotEqual();
                    break;

                // Binary comparison operations
                case LoxOpcode.GREATER:
                    Greater();
                    break;
                case LoxOpcode.GREATER_EQUALS:
                    GreaterEqual();
                    break;
                case LoxOpcode.LESS:
                    Less();
                    break;
                case LoxOpcode.LESS_EQUALS:
                    LessEqual();
                    break;

                // Control flow
                case LoxOpcode.RETURN:
                    return Return();

                default:
                    throw new LoxUnknownOpcodeException($"Unknown instruction {(byte)instruction}");
            }
        }
    }

    /// <summary>
    /// Reads the next bytecode
    /// </summary>
    /// <returns>Next bytecode value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe byte ReadByte() => *this.instructionPointer++;

    /// <summary>
    /// Frees allocated resources
    /// </summary>
    private unsafe void FreeResources()
    {
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (this.bytecode != null)
        {
            Marshal.FreeHGlobal((IntPtr)this.bytecode);
            this.bytecode           = null;
            this.instructionPointer = null;
        }

        this.stack?.Dispose();
        foreach (IntPtr alloc in this.allocations)
        {
            Marshal.FreeHGlobal(alloc);
        }
        this.currentChunk?.Dispose();
        this.allocations.Clear();
        // ReSharper enable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        FreeResources();
        this.IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Static methods
    /// <summary>
    /// Prints the given value
    /// </summary>
    /// <param name="value">Value to print</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintValue(in LoxValue value) => Console.WriteLine(value.ToString());
    #endregion
}
