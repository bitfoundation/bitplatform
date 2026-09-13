namespace Bit.BlazorUI;

internal static class BitLinkRelUtils
{
    internal static readonly BitLinkRels[] AllRels = Enum.GetValues<BitLinkRels>();

    internal static string GetRels(BitLinkRels rel)
    {
        return string.Join(" ", AllRels.Where(r => rel.HasFlag(r)).Select(GetRelName));
    }

    private static string GetRelName(BitLinkRels rel) => rel switch
    {
        // The multi-word rel values are hyphenated in HTML, which a plain lowercasing of the member name cannot produce.
        BitLinkRels.PrivacyPolicy => "privacy-policy",
        BitLinkRels.TermsOfService => "terms-of-service",
        _ => rel.ToString().ToLowerInvariant()
    };
}
