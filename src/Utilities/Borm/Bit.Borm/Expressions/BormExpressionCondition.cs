namespace Bit.Borm;

/// <summary>
/// A condition written as an expression — <c>{Age} &gt;= 18 and {Country} = 'US'</c>. Unlike a
/// <see cref="BormPredicateCondition"/> it is plain text, so it survives a round trip through JSON
/// (as a string in <c>x-visibleWhen</c> and its siblings), and unlike a
/// <see cref="BormFieldCondition"/> it can combine any number of fields and functions.
/// </summary>
public sealed class BormExpressionCondition : IBormCondition
{
    public BormExpressionCondition(BormExpression expression)
        => Expression = expression ?? throw new ArgumentNullException(nameof(expression));

    /// <summary>Parses <paramref name="expression"/> into a condition.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormExpressionCondition(string expression) : this(BormExpression.Parse(expression)) { }

    /// <summary>The expression behind the condition.</summary>
    public BormExpression Expression { get; }

    /// <inheritdoc />
    public IEnumerable<string> Dependencies => Expression.Dependencies;

    /// <inheritdoc />
    public bool Evaluate(IBormDataReader data) => Expression.IsTrue(data);

    public override string ToString() => Expression.Text;
}
