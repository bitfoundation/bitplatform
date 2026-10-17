using System.Linq.Expressions;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>Replaces the placeholder with a concrete queryable over the plan's rows.</summary>
internal sealed class BlazeDbRowsPlaceholderReplacer : ExpressionVisitor
{
    private readonly Expression _rows;

    public BlazeDbRowsPlaceholderReplacer(Expression rows) => _rows = rows;

    public override Expression? Visit(Expression? node) =>
        node is BlazeDbRowsPlaceholder ? _rows : base.Visit(node);
}
