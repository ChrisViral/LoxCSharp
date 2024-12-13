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
public class VirtualMachine(LoxChunk chunk)
{
    private unsafe byte* bytecode;
    private unsafe byte* instructionPointer;

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
    private InterpretResult Run()
    {
        while (true)
        {
            #if DEBUG_TRACE
            unsafe { BytecodeUtils.PrintInstruction(chunk, this.instructionPointer, (int)(this.instructionPointer - this.bytecode)); }
            #endif

            Opcode instruction = (Opcode)ReadByte();
            switch (instruction)
            {
                case Opcode.NOP:
                    break;

                case Opcode.CONSTANT:
                    PrintValue(ReadConstant());
                    break;

                case Opcode.CONSTANT_LONG:
                    PrintValue(ReadConstant(true));
                    break;

                case Opcode.RETURN:
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
    private unsafe byte ReadByte() => *this.instructionPointer++;

    /// <summary>
    /// Reads the next constant in the bytecode
    /// </summary>
    /// <param name="isLong">If the constant is stored with 24bits instead of 8</param>
    /// <returns>Stored constant value</returns>
    private LoxValue ReadConstant(in bool isLong = false)
    {
        int index;
        if (isLong)
        {
            byte a = ReadByte();
            byte b = ReadByte();
            byte c = ReadByte();
            index = BitConverter.ToInt32([a, b, c, 0]);
        }
        else
        {
            index = ReadByte();
        }

        return chunk.GetConstant(index);
    }

    /// <summary>
    /// Prints the given value
    /// </summary>
    /// <param name="value">Value to print</param>
    private static void PrintValue(in LoxValue value) => Console.WriteLine(value.ToString());
}
