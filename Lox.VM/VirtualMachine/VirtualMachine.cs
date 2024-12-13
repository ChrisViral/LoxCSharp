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
    private Stack stack;

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
                BytecodeUtils.PrintInstruction(chunk, this.instructionPointer, (int)(this.instructionPointer - this.bytecode));
                this.stack.PrintStack();
            }
            #endif

            Opcode instruction = (Opcode)ReadByte();
            switch (instruction)
            {
                case Opcode.NOP:
                    break;

                case Opcode.CONSTANT:
                    this.stack.Push(ReadConstant());
                    break;

                case Opcode.CONSTANT_LONG:
                    this.stack.Push(ReadLongConstant());
                    break;

                case Opcode.RETURN:
                    PrintValue(this.stack.Pop());
                    return InterpretResult.SUCCESS;

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
    /// Reads the next constant in the bytecode
    /// </summary>
    /// <returns>Stored constant value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LoxValue ReadConstant() => chunk.GetConstant(ReadByte());

    /// <summary>
    /// Reads the next 24bit constant in the bytecode
    /// </summary>
    /// <returns>Stored constant value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LoxValue ReadLongConstant()
    {
        byte a = ReadByte();
        byte b = ReadByte();
        byte c = ReadByte();
        int index = BitConverter.ToInt32([a, b, c, 0]);
        return chunk.GetConstant(index);
    }

    /// <summary>
    /// Prints the given value
    /// </summary>
    /// <param name="value">Value to print</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintValue(in LoxValue value) => Console.WriteLine(value.ToString());
}
