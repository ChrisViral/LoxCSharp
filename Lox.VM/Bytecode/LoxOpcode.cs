using FastEnumUtility;
using JetBrains.Annotations;

namespace Lox.VM.Bytecode;

public enum LoxOpcode : byte
{
    // No op
    NOP = 0,

    // Pop value from stack
    POP,

    // Constants
    CONSTANT_8,
    CONSTANT_16,
    CONSTANT_24,

    // Define global
    DEF_GLOBAL_8,
    DEF_GLOBAL_16,
    DEF_GLOBAL_24,
    // Define uninitialized global
    NDF_GLOBAL_8,
    NDF_GLOBAL_16,
    NDF_GLOBAL_24,
    // Get global
    GET_GLOBAL_8,
    GET_GLOBAL_16,
    GET_GLOBAL_24,
    // Get global
    SET_GLOBAL_8,
    SET_GLOBAL_16,
    SET_GLOBAL_24,

    // Literals
    NIL,
    TRUE,
    FALSE,
    ZERO,
    ONE,

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
    RETURN,
    PRINT
}

[FastEnum<LoxOpcode>, UsedImplicitly]
internal sealed partial class LoxOpcodeBooster;