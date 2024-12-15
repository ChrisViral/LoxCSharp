using System.Text;
using FastEnumUtility;
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
    /// Prints a given instruction
    /// </summary>
    /// <param name="chunk">Chunk the instruction is taken from</param>
    /// <param name="instructionPointer">Current instruction pointer</param>
    /// <param name="offset">Current instruction offset</param>
    public static unsafe void PrintInstruction(LoxChunk chunk, byte* instructionPointer, int offset)
    {
        LoxOpcode instruction = (LoxOpcode)(*instructionPointer);
        int line = chunk.GetLine(offset);
        BytecodePrinter.Append($"{offset:D4} {line,4} ");
        switch (instruction)
        {
            case LoxOpcode.NOP:
            case LoxOpcode.NIL:
            case LoxOpcode.TRUE:
            case LoxOpcode.FALSE:
            case LoxOpcode.NOT:
            case LoxOpcode.NEGATE:
            case LoxOpcode.ADD:
            case LoxOpcode.SUBTRACT:
            case LoxOpcode.MULTIPLY:
            case LoxOpcode.DIVIDE:
            case LoxOpcode.EQUALS:
            case LoxOpcode.NOT_EQUALS:
            case LoxOpcode.GREATER:
            case LoxOpcode.GREATER_EQUALS:
            case LoxOpcode.LESS:
            case LoxOpcode.LESS_EQUALS:
            case LoxOpcode.RETURN:
                PrintSimpleInstruction(instruction);
                break;

            case LoxOpcode.CONSTANT_8:
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_8, *(instructionPointer + 1));
                break;

            case LoxOpcode.CONSTANT_16:
            {
                byte a = *(instructionPointer + 1);
                byte b = *(instructionPointer + 2);
                int index = BitConverter.ToUInt16([a, b]);
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_16, index);
                break;
            }

            case LoxOpcode.CONSTANT_24:
            {
                byte a = *(instructionPointer + 1);
                byte b = *(instructionPointer + 2);
                byte c = *(instructionPointer + 3);
                int index = BitConverter.ToInt32([a, b, c, 0]);
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_24, index);
                break;
            }

            default:
                BytecodePrinter.AppendLine($"Unknown opcode {(byte)instruction}");
                break;
        }

        string result = BytecodePrinter.ToString();
        Console.Write(result);
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
        (LoxOpcode instruction, int offset, int line) = enumerator.CurrentInstruction;
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
            case LoxOpcode.NOP:
            case LoxOpcode.NIL:
            case LoxOpcode.TRUE:
            case LoxOpcode.FALSE:
            case LoxOpcode.NOT:
            case LoxOpcode.NEGATE:
            case LoxOpcode.ADD:
            case LoxOpcode.SUBTRACT:
            case LoxOpcode.MULTIPLY:
            case LoxOpcode.DIVIDE:
            case LoxOpcode.EQUALS:
            case LoxOpcode.NOT_EQUALS:
            case LoxOpcode.GREATER:
            case LoxOpcode.GREATER_EQUALS:
            case LoxOpcode.LESS:
            case LoxOpcode.LESS_EQUALS:
            case LoxOpcode.RETURN:
                PrintSimpleInstruction(instruction);
                break;

            case LoxOpcode.CONSTANT_8:
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_8, enumerator.NextByte());
                break;

            case LoxOpcode.CONSTANT_16:
            {
                byte a = enumerator.NextByte();
                byte b = enumerator.NextByte();
                int index = BitConverter.ToUInt16([a, b]);
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_16, index);
                break;
            }

            case LoxOpcode.CONSTANT_24:
            {
                byte a = enumerator.NextByte();
                byte b = enumerator.NextByte();
                byte c = enumerator.NextByte();
                int index = BitConverter.ToInt32([a, b, c, 0]);
                PrintConstantInstruction(chunk, LoxOpcode.CONSTANT_24, index);
                break;
            }

            default:
                BytecodePrinter.AppendLine($"Unknown opcode {(byte)instruction}");
                break;
        }
    }

    /// <summary>
    /// Prints the contents of a simple instruction
    /// </summary>
    /// <param name="instruction">Instruction to print</param>
    private static void PrintSimpleInstruction(LoxOpcode instruction) => BytecodePrinter.AppendLine(FastEnum.ToString<LoxOpcode, LoxOpcodeBooster>(instruction));

    /// <summary>
    /// Prints the contents of a constant instruction
    /// </summary>
    /// <param name="chunk">Current chunk</param>
    /// <param name="opcode">Constant opcode</param>
    /// <param name="index">Constant index</param>
    private static void PrintConstantInstruction(LoxChunk chunk, LoxOpcode opcode, int index)
    {
        BytecodePrinter.AppendLine($"{FastEnum.ToString<LoxOpcode, LoxOpcodeBooster>(opcode),-16} {index:D4} '{chunk.GetConstant(index)}'");
    }
    #endregion
}
