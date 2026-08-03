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
        /// filterColumn has a value, so multiple NULLs remain allowed.
        /// </summary>
        public IndexBuilder<T> HasUniqueIndexOnNullable(
            Expression<Func<T, object?>> property,
            Expression<Func<T, object?>>? filterColumn = null)
        {
            var index = builder.HasIndex(property).IsUnique();

            //#if (database != "MySql")
            var columnName = GetMemberName(filterColumn ?? property);
            //#if (database == "PostgreSQL")
            index.HasFilter($"\"{columnName}\" IS NOT NULL");
            //#elseif (database != "PostgreSQL")
            index.HasFilter($"[{columnName}] IS NOT NULL");
            //#endif
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
