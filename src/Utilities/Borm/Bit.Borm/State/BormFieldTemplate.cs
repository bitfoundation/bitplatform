namespace Bit.Borm;

/// <summary>
/// One field of the schema and where it sits: names, with <c>null</c> standing for "every row of the
/// list so far", so <c>["Lines", null, "Total"]</c> is every line's total.
/// </summary>
internal sealed class BormFieldTemplate(BormFieldDefinition field, string?[] pieces)
{
    public BormFieldDefinition Field { get; } = field;

    private readonly string?[] _pieces = pieces;

    /// <summary>
    /// The live paths of this field, in row order, given how many rows each list holds right now.
    /// A field outside any list has exactly one path.
    /// </summary>
    public void Expand(Func<string, int> count, List<(BormFieldDefinition Field, string Path)> into)
        => Expand(count, 0, string.Empty, into);

    private void Expand(Func<string, int> count, int at, string prefix, List<(BormFieldDefinition, string)> into)
    {
        for (var i = at; i < _pieces.Length; i++)
        {
            if (_pieces[i] is { } name)
            {
                // The first piece is a top-level field's own name, which may itself contain dots
                // (`x => x.Address.City`); joined as written, it is exactly the path the walk produced.
                prefix = prefix.Length == 0 ? name : BormPath.Combine(prefix, name);
                continue;
            }

            var rows = count(prefix);
            for (var row = 0; row < rows; row++)
                Expand(count, i + 1, BormPath.Combine(prefix, row), into);
            return;
        }
        into.Add((Field, prefix));
    }
}
