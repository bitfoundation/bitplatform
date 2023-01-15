using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.Nav;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavChild
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    private int _depth;

    private IDictionary<BitNavItemAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavItemAriaCurrent, string>()
    {
        [BitNavItemAriaCurrent.Page] = "page",
        [BitNavItemAriaCurrent.Step] = "step",
        [BitNavItemAriaCurrent.Location] = "location",
        [BitNavItemAriaCurrent.Time] = "time",
        [BitNavItemAriaCurrent.Date] = "date",
        [BitNavItemAriaCurrent.True] = "true"
    };

    [CascadingParameter] protected BitNav ParentBitNav { get; set; } = default!;

    [Parameter] public BitNavItem Item { get; set; } = default!;

    private string GetItemClasses(BitNavItem item)
    {
        var enabledClass = item.IsEnabled ? "enabled" : "disabled";
        var hasUrlClass = item.Url.HasNoValue() ? "nourl" : "hasurl";

        var isSelected = item == ParentBitNav.SelectedItem ? "selected" : "";

        return $"link-{enabledClass}-{hasUrlClass} {isSelected}";
    }

    private static bool IsRelativeUrl(string url)
    {
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }
}
