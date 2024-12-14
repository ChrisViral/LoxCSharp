using FastEnumUtility;

namespace Lox.VM.Bytecode;

public enum LoxOpcode : byte
{
    // No op
    NOP = 0,

    // Constants
    CONSTANT,
    CONSTANT_LONG,

    // Unary operations
    NEGATE,

    // Binary operations
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,

    // Control flow
    RETURN,
}

[FastEnum<LoxOpcode>]
internal sealed partial class LoxOpcodeBooster;