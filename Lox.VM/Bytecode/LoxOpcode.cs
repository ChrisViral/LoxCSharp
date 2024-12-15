using FastEnumUtility;
using JetBrains.Annotations;

namespace Lox.VM.Bytecode;

public enum LoxOpcode : byte
{
    // No op
    NOP = 0,

    // Constants
    CONSTANT_8,
    CONSTANT_16,
    CONSTANT_24,

    // Literals
    NIL,
    TRUE,
    FALSE,

    // Unary operations
    NOT,
    NEGATE,

    // Binary mathematical operations
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,

    // Binary equality operations
    EQUALS,
    NOT_EQUALS,

    // Binary comparison operations
    GREATER,
    GREATER_EQUALS,
    LESS,
    LESS_EQUALS,

    // Control flow
    RETURN
}

[FastEnum<LoxOpcode>, UsedImplicitly]
internal sealed partial class LoxOpcodeBooster;