using System.Collections;

namespace Bit.Borm;

/// <summary>
/// A reference to a field: <c>{Address.City}</c>, or with <c>[]</c> standing for every row of a list,
/// <c>{Lines[].Total}</c>, which reads as the list of every row's value.
/// </summary>
internal sealed class BormFieldNode(string path) : BormExpressionNode
{
    public string Path { get; } = path;

    private readonly int _wildcard = path.IndexOf("[]", StringComparison.Ordinal);

    public override object? Evaluate(IBormDataReader data)
        => _wildcard < 0 ? data.GetValue(Path) : Expand(data, Path);

    public override void CollectPaths(List<string> paths) => paths.Add(Path);

    /// <summary>
    /// Reads every row of the list before the first <c>[]</c>, continuing with the rest of the path in
    /// each — itself possibly another <c>[]</c>, so a list of lists flattens into one list of values.
    /// The list is found the way any reference is, relative to the owning object first; the rows are
    /// then read at the absolute path it was found at.
    /// </summary>
    private static List<object?> Expand(IBormDataReader data, string path)
    {
        var results = new List<object?>();
        var wildcard = path.IndexOf("[]", StringComparison.Ordinal);
        var (store, listPath) = BormScopedDataReader.Resolve(data, path[..wildcard]);
        Walk(store, listPath, path[(wildcard + 2)..], results);
        return results;

        static void Walk(IBormDataReader store, string listPath, string rest, List<object?> into)
        {
            var count = store.GetValue(listPath) switch
            {
                ICollection c => c.Count,
                IEnumerable e and not string => e.Cast<object?>().Count(),
                _ => 0
            };

            var next = rest.IndexOf("[]", StringComparison.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var item = BormPath.Combine(listPath, i);
                if (next < 0)
                {
                    into.Add(store.GetValue(item + rest));
                    continue;
                }
                Walk(store, item + rest[..next], rest[(next + 2)..], into);
            }
        }
    }
}
