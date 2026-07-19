//+:cnd:noEmit
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore;

public static class EntityTypeBuilderExtensions
{
    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        /// <summary>
        /// Adds a filtered unique index (single or composite) whose uniqueness only applies to the rows where
        /// <paramref name="filterColumn"/> has a value (multiple NULLs remain allowed). The column identifier in the
        /// filter is quoted per database provider.
        /// </summary>
        /// <param name="property">The indexed column(s), e.g. <c>t => t.Domain</c> or <c>t => new { t.A, t.B }</c>.</param>
        /// <param name="filterColumn">
        /// The column the filter tests. Defaults to <paramref name="property"/>, which only works for a single-column index;
        /// a composite index must pass this explicitly (e.g. index on <c>new { A, TenantId }</c>, filter on <c>t => t.TenantId</c>).
        /// </param>
        public IndexBuilder<T> HasUniqueIndexOnNullable(
            Expression<Func<T, object?>> property,
            Expression<Func<T, object?>>? filterColumn = null)
        {
            var columnName = GetMemberName(filterColumn ?? property);

            var index = builder.HasIndex(property).IsUnique();

            //#if (database == "PostgreSQL")
            index.HasFilter($"\"{columnName}\" IS NOT NULL");
            //#else
            index.HasFilter($"[{columnName}] IS NOT NULL");
            //#endif

            return index;
        }
    }

    /// <summary>
    /// Extracts the property name from a <c>t => t.Property</c> expression, unwrapping the <see cref="ExpressionType.Convert"/>
    /// node the compiler inserts when the property type differs from <see cref="object"/> (boxing for value types,
    /// reference conversion for reference types).
    /// </summary>
    private static string GetMemberName<T>(Expression<Func<T, object?>> expression)
    {
        var body = expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : expression.Body;

        if (body is MemberExpression member)
            return member.Member.Name;

        throw new ArgumentException("Expression must be a simple property access, e.g. t => t.Domain.", nameof(expression));
    }
}
