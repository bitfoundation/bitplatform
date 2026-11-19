namespace Bit.BevTools;

/// <summary>Cached names so attributing a render never calls <c>Enum.ToString</c>, which allocates.</summary>
internal static class BevToolsRenderCauseNames
{
    private static readonly string[] Names = Enum.GetNames<BevToolsRenderCause>();

    public static string Of(BevToolsRenderCause cause)
    {
        var index = (int)cause;
        return (uint)index < (uint)Names.Length ? Names[index] : cause.ToString();
    }
}
