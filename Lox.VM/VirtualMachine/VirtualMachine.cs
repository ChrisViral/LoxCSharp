﻿using System.Runtime.CompilerServices;
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
    ~VirtualMachine() => Dispose();
    #endregion

    #region Methods
    /// <summary>
    /// Starts this Lox VM interpreter
    /// </summary>
    /// <returns>Completion status of the interpreter</returns>
    /// <exception cref="InvalidOperationException">If the interpreter is already running</exception>
    public unsafe InterpretResult Run(LoxChunk chunk)
    {
        if (this.IsRunning) throw new InvalidOperationException("This VM is already running");
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        this.IsRunning    = true;
        this.currentChunk = chunk;
        ReadOnlySpan<byte> bytecodeSpan = chunk.AsSpan();
        IntPtr handle = Marshal.AllocHGlobal(bytecodeSpan.Length);
        this.stack = new Stack();
        try
        {
            this.bytecode           = (byte*)handle;
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
        finally
        {
            Marshal.FreeHGlobal(handle);
            this.stack.Dispose();
            this.stack              = null!;
            this.currentChunk       = null!;
            this.bytecode           = null;
            this.instructionPointer = null;
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        this.stack?.Dispose();

        foreach (IntPtr stringPtr in this.allocations)
        {
            Marshal.FreeBSTR(stringPtr);
        }
        this.allocations.Clear();
        this.currentChunk.Dispose();
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
