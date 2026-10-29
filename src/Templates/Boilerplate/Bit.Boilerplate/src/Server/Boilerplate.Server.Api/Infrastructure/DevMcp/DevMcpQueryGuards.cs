using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

public static partial class DevMcpQueryGuards
{
    // Literals, operators and type names: a path that opens with one of these names no column of the entity.
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "false", "null", "new", "and", "or", "not", "iif", "as",
        "int", "long", "string", "bool", "decimal", "double", "float", "guid",
        "datetime", "datetimeoffset", "timespan", "object", "convert", "parse",
        "byte", "short", "uint", "ulong", "char"
    };

    // Dynamic LINQ's own names for the row: "it.PasswordHash" IS "PasswordHash", so the prefix is stripped rather
    // than treated as a keyword - skipping the whole path would hand back every column this guard exists to refuse.
    private static readonly HashSet<string> SelfReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        "it", "this", "parent", "root"
    };

    public static string? Validate(IEntityType entityType, string[] select, string? filter, string? orderBy)
    {
        var entityError = DevMcpForbiddenColumns.RejectEntity(entityType);
        if (entityError is not null)
            return entityError;

        if (select is not { Length: > 0 } || select.All(string.IsNullOrWhiteSpace))
            return "A projection is required. Name the columns to return; selecting everything is refused.";

        foreach (var column in select)
        {
            var error = DevMcpForbiddenColumns.RejectProperty(entityType, column);
            if (error is not null)
                return error;
        }

        if (string.IsNullOrWhiteSpace(orderBy) is false)
        {
            foreach (var key in SplitOrderBy(orderBy))
            {
                var error = DevMcpForbiddenColumns.RejectProperty(entityType, StripSelfReferences(key));
                if (error is not null)
                    return error;
            }
        }

        if (string.IsNullOrWhiteSpace(filter) is false)
        {
            if (filter.Length > DevMcpLimits.MaxFilterLength)
                return $"A filter may be at most {DevMcpLimits.MaxFilterLength} characters long.";

            foreach (var path in ExtractPaths(filter))
            {
                var error = DevMcpForbiddenColumns.RejectProperty(entityType, path);
                if (error is not null)
                    return error;
            }
        }

        return null;
    }

    private static readonly HashSet<Type> ValueTypesAFilterMayBuild =
    [
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(Guid)
    ];

    public static string? RejectComputedValues(LambdaExpression predicate)
    {
        var finder = new ComputedValueFinder();
        finder.Visit(predicate.Body);
        return finder.Error;
    }

    private sealed class ComputedValueFinder : ExpressionVisitor
    {
        public string? Error { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            if (Error is not null || node is null)
                return node;

            if (IsComputation(node) && IsAllowedComputation(node) is false && ParameterFinder.References(node) is false)
            {
                Error = $"The filter builds a {node.Type.Name} value of its own. Compare columns with literals instead.";
                return node;
            }

            return base.Visit(node);
        }

        private static bool IsComputation(Expression node)
            => node is MethodCallExpression or NewExpression or InvocationExpression or ListInitExpression or MemberInitExpression
               || node.NodeType is ExpressionType.NewArrayBounds;

        private static bool IsAllowedComputation(Expression node) => node switch
        {
            NewExpression newExpression => ValueTypesAFilterMayBuild.Contains(newExpression.Type),
            MethodCallExpression call => call.Method.DeclaringType is Type declaringType && ValueTypesAFilterMayBuild.Contains(declaringType) && call.Type.IsValueType,
            _ => false
        };
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        private bool found;

        public static bool References(Expression node)
        {
            var finder = new ParameterFinder();
            finder.Visit(node);
            return finder.found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            found = true;
            return node;
        }
    }

    public static IEnumerable<string> SplitOrderBy(string orderBy)
    {
        foreach (var part in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var key = OrderByDirection().Replace(part, "").Trim();
            if (key.Length > 0)
                yield return key;
        }
    }

    public static IEnumerable<string> ExtractPaths(string expression)
    {
        var withoutStrings = StripQuoted(expression);
        foreach (Match match in IdentifierPath().Matches(withoutStrings))
        {
            var path = StripSelfReferences(match.Value);

            if (path.Length == 0 || Keywords.Contains(path.Split('.')[0]))
                continue;

            yield return path;
        }
    }

    /// <summary>"it.Email" is "Email". Empty when the path names the row and no column of it.</summary>
    public static string StripSelfReferences(string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var start = 0;
        while (start < segments.Length && SelfReferences.Contains(segments[start]))
            start++;

        return start == segments.Length ? "" : string.Join('.', segments[start..]);
    }

    private static string StripQuoted(string expression)
    {
        return QuotedLiteral().Replace(expression, " ");
    }

    [GeneratedRegex("""(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')""", RegexOptions.Singleline)]
    private static partial Regex QuotedLiteral();

    [GeneratedRegex(@"\b[_A-Za-z][_A-Za-z0-9]*(?:\.[_A-Za-z][_A-Za-z0-9]*)*")]
    private static partial Regex IdentifierPath();

    [GeneratedRegex(@"\b(asc|desc)\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrderByDirection();
}
