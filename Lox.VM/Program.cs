using Lox.VM;
using Lox.VM.Bytecode;
using Lox.VM.Runtime;

LoxChunk chunk = new();

chunk.AddConstant(1.2d, 123);
chunk.AddConstant(3.4d, 123);
chunk.AddOpcode(LoxOpcode.ADD, 123);
chunk.AddConstant(5.6d, 123);
chunk.AddOpcode(LoxOpcode.DIVIDE, 123);
chunk.AddOpcode(LoxOpcode.NEGATE, 123);
chunk.AddOpcode(LoxOpcode.RETURN, 123);

VirtualMachine virtualMachine = new(chunk);
InterpretResult result = virtualMachine.Interpret();

Environment.Exit((int)result);
