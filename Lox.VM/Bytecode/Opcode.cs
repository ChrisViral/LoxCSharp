namespace Lox.VM.Bytecode;

public enum Opcode : byte
{
    OP_NOP,
    OP_CONSTANT,
    OP_CONSTANT_LONG,
    OP_RETURN,
}