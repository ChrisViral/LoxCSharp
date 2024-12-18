using System.Runtime.CompilerServices;
using System.Text;
using FastEnumUtility;
using Lox.VM.Bytecode;

namespace Lox.VM.Utils;

/// <summary>
/// Bytecode utility
/// </summary>
public static class BytecodePrinter
{
    #region Printing utils
    /// <summary>
    /// Internal builder
    /// </summary>
    private static readonly StringBuilder BytecodeStringBuilder = new();

    /// <summary>
    /// Prints the contents of the given chunk
    /// </summary>
    /// <param name="chunk">Chunk to print</param>
    /// <param name="name">Chunk name</param>
    public static void PrintChunk(LoxChunk chunk, string name)
    {
        BytecodeStringBuilder.AppendLine($"== {name} ==");
        int previousLine = -1;
        LoxChunk.BytecodeEnumerator enumerator = chunk.GetBytecodeEnumerator();
        while (enumerator.MoveNext())
        {
            int currentLine = enumerator.Current.line;
            bool newLine = currentLine != previousLine;
            previousLine = currentLine;
            PrintInstruction(chunk, ref enumerator, newLine);
        }
        Console.Write(BytecodeStringBuilder.ToString());
        BytecodeStringBuilder.Clear();
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
        BytecodeStringBuilder.Append($"{offset:D6} {line,6} ");
        switch (instruction)
        {
            case LoxOpcode.NOP:
            case LoxOpcode.POP:
            case LoxOpcode.NIL:
            case LoxOpcode.TRUE:
            case LoxOpcode.FALSE:
            case LoxOpcode.ZERO:
            case LoxOpcode.ONE:
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
            case LoxOpcode.PRINT:
                PrintSimpleInstruction(instruction);
                break;

            case LoxOpcode.POPN:
            case LoxOpcode.GET_LOCAL:
            case LoxOpcode.SET_LOCAL:
                PrintOperandInstruction(instruction, *(instructionPointer + 1));
                break;

            case LoxOpcode.POPN_16:
            case LoxOpcode.GET_LOCAL_16:
            case LoxOpcode.SET_LOCAL_16:
            case LoxOpcode.JUMP:
            case LoxOpcode.JUMP_FALSE:
            {
                ushort operand = Unsafe.ReadUnaligned<ushort>(instructionPointer + 1);
                PrintOperandInstruction(instruction, operand);
                break;
            }

            case LoxOpcode.CONSTANT:
            case LoxOpcode.DEF_GLOBAL:
            case LoxOpcode.NDF_GLOBAL:
            case LoxOpcode.GET_GLOBAL:
            case LoxOpcode.SET_GLOBAL:
                PrintConstantInstruction(chunk, instruction, *(instructionPointer + 1));
                break;

            case LoxOpcode.CONSTANT_16:
            case LoxOpcode.DEF_GLOBAL_16:
            case LoxOpcode.NDF_GLOBAL_16:
            case LoxOpcode.GET_GLOBAL_16:
            case LoxOpcode.SET_GLOBAL_16:
            {
                ushort index = Unsafe.ReadUnaligned<ushort>(instructionPointer + 1);
                PrintConstantInstruction(chunk, instruction, index);
                break;
            }

            default:
                BytecodeStringBuilder.AppendLine($"Unknown opcode {instruction}");
                break;
        }

        string result = BytecodeStringBuilder.ToString();
        Console.Write(result);
        BytecodeStringBuilder.Clear();
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
            BytecodeStringBuilder.Append($"{offset:D6} {line,6} ");
        }
        else
        {
            BytecodeStringBuilder.Append($"{offset:D6}      | ");
        }

        switch (instruction)
        {
            case LoxOpcode.NOP:
            case LoxOpcode.POP:
            case LoxOpcode.NIL:
            case LoxOpcode.TRUE:
            case LoxOpcode.FALSE:
            case LoxOpcode.ZERO:
            case LoxOpcode.ONE:
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
            case LoxOpcode.PRINT:
                PrintSimpleInstruction(instruction);
                break;

            case LoxOpcode.POPN:
            case LoxOpcode.GET_LOCAL:
            case LoxOpcode.SET_LOCAL:
                PrintOperandInstruction(instruction, enumerator.NextByte());
                break;

            case LoxOpcode.POPN_16:
            case LoxOpcode.GET_LOCAL_16:
            case LoxOpcode.SET_LOCAL_16:
            case LoxOpcode.JUMP:
            case LoxOpcode.JUMP_FALSE:
            {
                byte a = enumerator.NextByte();
                byte b = enumerator.NextByte();
                ushort operand = BitConverter.ToUInt16([a, b]);
                PrintOperandInstruction(instruction, operand);
                break;
            }

            case LoxOpcode.CONSTANT:
            case LoxOpcode.DEF_GLOBAL:
            case LoxOpcode.NDF_GLOBAL:
            case LoxOpcode.GET_GLOBAL:
            case LoxOpcode.SET_GLOBAL:
                PrintConstantInstruction(chunk, instruction, enumerator.NextByte());
                break;

            case LoxOpcode.CONSTANT_16:
            case LoxOpcode.DEF_GLOBAL_16:
            case LoxOpcode.NDF_GLOBAL_16:
            case LoxOpcode.GET_GLOBAL_16:
            case LoxOpcode.SET_GLOBAL_16:
            {
                byte a = enumerator.NextByte();
                byte b = enumerator.NextByte();
                ushort index = BitConverter.ToUInt16([a, b]);
                PrintConstantInstruction(chunk, instruction, index);
                break;
            }

            default:
                BytecodeStringBuilder.AppendLine($"Unknown opcode {instruction}");
                break;
        }
    }

    /// <summary>
    /// Prints the contents of a simple instruction
    /// </summary>
    /// <param name="instruction">Instruction to print</param>
    private static void PrintSimpleInstruction(LoxOpcode instruction)
    {
        BytecodeStringBuilder.AppendLine(FastEnum.ToString<LoxOpcode, LoxOpcodeBooster>(instruction));
    }

    /// <summary>
    /// Prints an instruction with it's operand
    /// </summary>
    /// <param name="instruction">Instruction to print</param>
    /// <param name="operand">Operand value</param>
    private static void PrintOperandInstruction(LoxOpcode instruction, ushort operand)
    {
        BytecodeStringBuilder.AppendLine($"{FastEnum.ToString<LoxOpcode, LoxOpcodeBooster>(instruction),-16} {operand:D5}");
    }

    /// <summary>
    /// Prints the contents of a constant instruction
    /// </summary>
    /// <param name="chunk">Current chunk</param>
    /// <param name="instruction">Constant opcode</param>
    /// <param name="index">Constant index</param>
    private static void PrintConstantInstruction(LoxChunk chunk, LoxOpcode instruction, ushort index)
    {
        BytecodeStringBuilder.AppendLine($"{FastEnum.ToString<LoxOpcode, LoxOpcodeBooster>(instruction),-16} {index:D5} '{chunk.GetConstant(index)}'");
    }
    #endregion
}
