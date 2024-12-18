using FastEnumUtility;
using JetBrains.Annotations;

namespace Lox.VM.Bytecode;

public enum LoxOpcode : byte
{
    // No op
    NOP = 0,

    // Pop value from stack
    POP,
    POPN,
    POPN_16,

    // Constants
    CONSTANT,
    CONSTANT_16,

    // Define global
    DEF_GLOBAL,
    DEF_GLOBAL_16,
    // Define uninitialized global
    NDF_GLOBAL,
    NDF_GLOBAL_16,
    // Get global
    GET_GLOBAL,
    GET_GLOBAL_16,
    // Get global
    SET_GLOBAL,
    SET_GLOBAL_16,

    // Get local
    GET_LOCAL,
    GET_LOCAL_16,
    // Set local
    SET_LOCAL,
    SET_LOCAL_16,

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

    // Print
    PRINT,

    // Control flow
    JUMP,
    JUMP_TRUE,
    JUMP_FALSE,
    RETURN
}

[FastEnum<LoxOpcode>, UsedImplicitly]
internal sealed partial class LoxOpcodeBooster;