using System.Globalization;

namespace Bit.BevTools.Demo.Client.Services;

/// <summary>
/// The culture both hosts format with. The demo builds with InvariantGlobalization, whose currency sign is the generic
/// "¤"; the made-up prices read as dollars instead, everything else stays invariant.
/// </summary>
public static class DemoCulture
{
    public static void Apply()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "$";
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
    }
}
