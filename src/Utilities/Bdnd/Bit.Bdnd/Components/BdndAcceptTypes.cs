namespace Bit.Bdnd;

/// <summary>Reads the item types a component takes from its <c>Accept</c> and <c>AcceptTypes</c> parameters.</summary>
internal static class BdndAcceptTypes
{
    /// <summary>
    /// <paramref name="accept"/> and <paramref name="acceptTypes"/> together, without empty names or repeats, in the
    /// order given.
    /// </summary>
    public static string[] Of(string? accept, IEnumerable<string>? acceptTypes)
    {
        var types = new List<string>();
        if (string.IsNullOrEmpty(accept) is false) types.Add(accept);
        if (acceptTypes is not null) types.AddRange(acceptTypes.Where(t => string.IsNullOrEmpty(t) is false));
        return types.Distinct(StringComparer.Ordinal).ToArray();
    }
}
