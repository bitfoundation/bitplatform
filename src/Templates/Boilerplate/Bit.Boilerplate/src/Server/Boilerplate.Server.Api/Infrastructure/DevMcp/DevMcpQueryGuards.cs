using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

public static partial class DevMcpQueryGuards
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "false", "null", "new", "and", "or", "not", "iif", "as",
        "int", "long", "string", "bool", "decimal", "double", "float", "guid",
        "datetime", "datetimeoffset", "timespan", "object", "convert", "parse",
        "it", "parent", "root", "this", "byte", "short", "uint", "ulong", "char"
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
                var error = DevMcpForbiddenColumns.RejectProperty(entityType, key);
                if (error is not null)
                    return error;
            }
        }

        if (string.IsNullOrWhiteSpace(filter) is false)
        {
            foreach (var path in ExtractPaths(filter))
            {
                if (Keywords.Contains(path.Replace(".", "")))
                    continue;

                var error = DevMcpForbiddenColumns.RejectProperty(entityType, path);
                if (error is not null)
                    return error;
            }
        }

        return null;
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
            var path = match.Value;
            var first = path.Split('.')[0];
            if (Keywords.Contains(first))
                continue;
            yield return path;
        }
    }

    private static string StripQuoted(string expression)
    {
        return QuotedLiteral().Replace(expression, " ");
    }

    [GeneratedRegex("""(?<q>["'])(?:\\.|(?!\k<q>).)*\k<q>""", RegexOptions.Singleline)]
    private static partial Regex QuotedLiteral();

    [GeneratedRegex(@"\b[_A-Za-z][_A-Za-z0-9]*(?:\.[_A-Za-z][_A-Za-z0-9]*)*")]
    private static partial Regex IdentifierPath();

    [GeneratedRegex(@"\b(asc|desc)\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrderByDirection();
}
