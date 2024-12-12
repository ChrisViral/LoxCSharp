using System.Text;
using Lox.Common.Utils;
using Lox.VM.Bytecode;

namespace Lox.VM.Utils;

/// <summary>
/// Bytecode utility
/// </summary>
public static class BytecodeUtils
{
    #region Printing utils
    /// <summary>
    /// Internal builder
    /// </summary>
    private static readonly StringBuilder BytecodePrinter = new();

    /// <summary>
    /// Prints the contents of the given chunk
    /// </summary>
    /// <param name="chunk">Chunk to print</param>
    /// <param name="name">Chunk name</param>
    public static void PrintChunk(LoxChunk chunk, string name)
    {
        BytecodePrinter.AppendLine($"== {name} ==");
        int previousLine = -1;
        LoxChunk.BytecodeEnumerator enumerator = chunk.GetBytecodeEnumerator();
        while (enumerator.MoveNext())
        {
            int currentLine = enumerator.Current.line;
            bool newLine = currentLine != previousLine;
            previousLine = currentLine;
            PrintInstruction(chunk, ref enumerator, newLine);
        }
        Console.Write(BytecodePrinter.ToString());
        BytecodePrinter.Clear();
    }

    /// <summary>
    /// Prints an instruction from the specified offset
    /// </summary>
    /// <param name="chunk">Code chunk</param>
    /// <param name="enumerator">Current bytecode enumerator</param>
    /// <param name="newLine">If the instruction is on a new line or not</param>
    private static void PrintInstruction(LoxChunk chunk, ref LoxChunk.BytecodeEnumerator enumerator, bool newLine)
    {
        (Opcode instruction, int offset, int line) = enumerator.CurrentInstruction;
        if (newLine)
        {
            BytecodePrinter.Append($"{offset:D4} {line,4} ");
        }
        else
        {
            BytecodePrinter.Append($"{offset:D4}    | ");
        }

        switch (instruction)
        {
            case Opcode.OP_NOP:
            case Opcode.OP_RETURN:
                PrintSimpleInstruction(instruction);
                break;

            case Opcode.OP_CONSTANT:
                PrintConstantInstruction(chunk, ref enumerator);
                break;

            case Opcode.OP_CONSTANT_LONG:
                PrintLongConstantInstruction(chunk, ref enumerator);
                break;

            default:
                BytecodePrinter.AppendLine($"Unknown opcode {(byte)instruction}");
                break;
        }
    }

    /// <summary>
    /// Prints the contents of a simple instruction
    /// </summary>
    /// <param name="instruction">Instruction to print</param>
    private static void PrintSimpleInstruction(Opcode instruction) => BytecodePrinter.AppendLine(EnumUtils.ToString(instruction));

    /// <summary>
    /// Prints the contents of a constant instruction
    /// </summary>
    /// <param name="chunk">Current chunk</param>
    /// <param name="enumerator">Current bytecode enumerator</param>
    private static void PrintConstantInstruction(LoxChunk chunk, ref LoxChunk.BytecodeEnumerator enumerator)
    {
        int index = enumerator.NextByte();
        BytecodePrinter.AppendLine($"{EnumUtils.ToString(Opcode.OP_CONSTANT),-16} {index:D4} '{chunk.GetConstant(index)}'");
    }

    /// <summary>
    /// Prints the contents of a constant instruction
    /// </summary>
    /// <param name="chunk">Current chunk</param>
    /// <param name="enumerator">Current bytecode enumerator</param>
    private static void PrintLongConstantInstruction(LoxChunk chunk, ref LoxChunk.BytecodeEnumerator enumerator)
    {
        byte a = enumerator.NextByte();
        byte b = enumerator.NextByte();
        byte c = enumerator.NextByte();
        int index = BitConverter.ToInt32([a, b, c, 0]);
        BytecodePrinter.AppendLine($"{EnumUtils.ToString(Opcode.OP_CONSTANT_LONG),-16} {index:D4} '{chunk.GetConstant(index)}'");
    }
    #endregion
}
