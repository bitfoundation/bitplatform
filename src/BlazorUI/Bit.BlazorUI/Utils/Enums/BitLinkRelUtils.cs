namespace Bit.BlazorUI;

internal class BitLinkRelUtils
{
    internal static BitLinkRel[] AllRels = Enum.GetValues<BitLinkRel>();

    internal static string GetRels(BitLinkRel rel)
    {
        return string.Join(" ", AllRels.Where(r => rel.HasFlag(r)).Select(r => r.ToString().ToLower()));
    }
}
