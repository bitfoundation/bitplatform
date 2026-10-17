using System.Linq.Expressions;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Stands in for the plan's result inside a rebuilt remainder expression, so the executor can
/// substitute the actual rows once the plan has run.
/// </summary>
internal sealed class BlazeDbRowsPlaceholder : Expression
{
    private BlazeDbRowsPlaceholder(Type type) => Type = type;

    public static BlazeDbRowsPlaceholder For(Type entityClrType) =>
        new(typeof(IQueryable<>).MakeGenericType(entityClrType));

    public override Type Type { get; }

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override bool CanReduce => false;

    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
}
