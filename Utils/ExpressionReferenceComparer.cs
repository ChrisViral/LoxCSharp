using System.Runtime.CompilerServices;
using Lox.Syntax.Expressions;

namespace Lox.Utils;

/// <summary>
/// Lox expression reference equality comparer
/// </summary>
public sealed class ExpressionReferenceComparer : IEqualityComparer<LoxExpression>
{
    /// <summary>
    /// Comparer instance
    /// </summary>
    public static ExpressionReferenceComparer Comparer { get; } = new();

    /// <summary>
    /// Prevents external instantiation
    /// </summary>
    private ExpressionReferenceComparer() { }

    /// <inheritdoc />
    public bool Equals(LoxExpression? x, LoxExpression? y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public int GetHashCode(LoxExpression obj) => RuntimeHelpers.GetHashCode(obj);
}
