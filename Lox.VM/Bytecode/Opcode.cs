namespace Lox.VM.Bytecode;

public enum Opcode : byte
{
    NOP,
    CONSTANT,
    CONSTANT_LONG,
    RETURN,
}