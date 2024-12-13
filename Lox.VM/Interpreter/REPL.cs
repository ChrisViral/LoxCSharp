using Lox.Common;
using Lox.VM.Scanner;

namespace Lox.VM;

public class REPL : LoxREPL<Token, LoxScanner, LoxInterpreter>;