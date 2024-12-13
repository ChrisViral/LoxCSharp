using JetBrains.Annotations;

namespace Lox.Common;

/// <summary>
/// Scanner interface
/// </summary>
/// <typeparam name="T"></typeparam>
[PublicAPI]
public interface ILoxScanner<out T> where T : struct
{
    /// <summary>
    /// Tokenizes a source code string
    /// </summary>
    /// <param name="source">Source code string</param>
    /// <returns>Tokens enumerable</returns>
    IEnumerable<T> Tokenize(string source);
}
