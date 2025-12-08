namespace Bit.BlazorUI;

internal class BitLinkRelUtils
{
    internal static BitLinkRels[] AllRels = Enum.GetValues<BitLinkRels>();

    internal static string GetRels(BitLinkRels rel)
    {
        return string.Join(" ", AllRels.Where(r => rel.HasFlag(r)).Select(r => r.ToString().ToLower()));
    }
}
