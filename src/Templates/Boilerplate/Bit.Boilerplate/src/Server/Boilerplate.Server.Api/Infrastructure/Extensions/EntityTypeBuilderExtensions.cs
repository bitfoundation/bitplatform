//+:cnd:noEmit
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore;

public static class EntityTypeBuilderExtensions
{
    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        /// <summary>
        /// Adds a unique index (single or composite) whose uniqueness only applies to the rows where
        /// <paramref name="filterColumn"/> has a value, so multiple NULLs remain allowed.
        /// <para>
        /// Every provider except MySQL gets that as a filtered index, with the column identifier quoted per provider.
        /// MySQL has no filtered indexes at all - <c>MySqlMigrationsSqlGenerator.IndexOptions</c> silently DROPS the
        /// filter - but it also treats NULLs as distinct in a plain unique index, which is exactly the semantics wanted
        /// here, so the filter is simply omitted there rather than emitted and thrown away.
        /// </para>
        /// <para>
        /// For <c>database == "Other"</c> the <c>[Bracket]</c> quoting of the <c>#else</c> branch is a SQL Server /
        /// SQLite convention; adjust it for your provider before generating the first migration.
        /// </para>
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
            var index = builder.HasIndex(property).IsUnique();

            //#if (database != "MySql")
            var columnName = GetMemberName(filterColumn ?? property);
            //#endif
            //#if (database == "PostgreSQL")
            index.HasFilter($"\"{columnName}\" IS NOT NULL");
            //#endif
            //#if (database != "PostgreSQL" && database != "MySql")
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
