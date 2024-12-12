using Lox.Common.Utils;

namespace Lox.VM.Utils;

public static class BytecodeUtils
{
    /// <summary>
    /// Prints the contents of the given chunk
    /// </summary>
    /// <param name="chunk">Chunk to print</param>
    /// <param name="name">Chunk name</param>
    public static void PrintChunk(LoxChunk chunk, string name)
    {
        Console.WriteLine($"== {name} ==");
        for (int offset = 0; offset < chunk.Count; )
        {
            PrintInstruction(chunk, ref offset);
        }
    }

    /// <summary>
    /// Prints an instruction from the specified offset
    /// </summary>
    /// <param name="chunk">Code chunk</param>
    /// <param name="offset">Current offset</param>
    private static void PrintInstruction(LoxChunk chunk, ref int offset)
    {
        Console.Write($"{offset:D4}\t");
        Opcode instruction = (Opcode)chunk[offset];
        switch (instruction)
        {
            case Opcode.OP_RETURN:
                PrintSimpleInstruction(instruction, ref offset);
                break;

            case Opcode.OP_CONSTANT:
                PrintConstantInstruction(chunk, ref offset);
                break;

            default:
                Console.WriteLine($"Unknown opcode {(byte)instruction}");
                offset++;
                break;
        }
    }

    /// <summary>
    /// Prints the contents of a simple instruction
    /// </summary>
    /// <param name="instruction">Instruction to print</param>
    /// <param name="offset">Current offset</param>
    private static void PrintSimpleInstruction(Opcode instruction, ref int offset)
    {
        Console.WriteLine(EnumUtils.ToString(instruction));
        offset++;
    }

    /// <summary>
    /// Prints the contents of a constant instruction
    /// </summary>
    /// <param name="chunk">Current chunk</param>
    /// <param name="offset">Current offset</param>
    private static void PrintConstantInstruction(LoxChunk chunk, ref int offset)
    {
        byte index = chunk[++offset];
        Console.Write($"{EnumUtils.ToString(Opcode.OP_CONSTANT),-16}{index:D4} ");
        Console.WriteLine(chunk.GetConstant(index).ToString());
        offset++;
    }
}
