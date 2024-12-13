using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.VM.Bytecode;
using Lox.VM.Exceptions;
using Lox.VM.Runtime;
using Lox.VM.Utils;

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
/// <param name="chunk">Lox code chunk to interpret</param>
[PublicAPI]
public partial class VirtualMachine(LoxChunk chunk)
{
    private unsafe byte* bytecode;
    private unsafe byte* instructionPointer;
    private Stack? stack;

    /// <summary>
    /// If the VM is currently running
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts this Lox VM interpreter
    /// </summary>
    /// <returns>Completion status of the interpreter</returns>
    /// <exception cref="InvalidOperationException">If the interpreter is already running</exception>
    public unsafe InterpretResult Interpret()
    {
        if (this.IsRunning) throw new InvalidOperationException("This VM is already running");

        this.IsRunning = true;
        ReadOnlySpan<byte> bytecodeSpan = chunk.AsSpan();
        IntPtr handle = Marshal.AllocHGlobal(bytecodeSpan.Length);
        this.stack = new Stack();
        try
        {
            this.bytecode           = (byte*)handle.ToPointer();
            this.instructionPointer = this.bytecode;
            using (UnmanagedMemoryStream stream = new(this.bytecode, bytecodeSpan.Length, bytecodeSpan.Length, FileAccess.Write))
            {
                stream.Write(bytecodeSpan);
            }

            return Run();
        }
        finally
        {
            Marshal.FreeHGlobal(handle);
            this.stack.Dispose();
            this.stack              = null;
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
                BytecodeUtils.PrintInstruction(chunk, this.instructionPointer, (int)(this.instructionPointer - this.bytecode));
            }
            #endif

            LoxOpcode instruction = (LoxOpcode)ReadByte();
            switch (instruction)
            {
                case LoxOpcode.NOP:
                    break;

                // Constants
                case LoxOpcode.CONSTANT:
                    ReadConstant();
                    break;
                case LoxOpcode.CONSTANT_LONG:
                    ReadLongConstant();
                    break;

                // Unary operations
                case LoxOpcode.NEGATE:
                    Negate();
                    break;

                // Binary operations
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
    /// Prints the given value
    /// </summary>
    /// <param name="value">Value to print</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintValue(in LoxValue value) => Console.WriteLine(value.ToString());
}
