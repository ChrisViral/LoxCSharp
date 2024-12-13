using JetBrains.Annotations;

namespace Lox.Common;

/// <summary>
/// Lox interpreter interface
/// </summary>
/// <typeparam name="T">Token type</typeparam>
[PublicAPI]
public interface ILoxInterpreter<in T> where T : struct
{
    /// <summary>
    /// Interprets a Lox program from specified source tokens
    /// </summary>
    /// <param name="tokens">Source tokens</param>
    void Interpret(IEnumerable<T> tokens);
}
