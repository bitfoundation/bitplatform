using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.NavList;

public partial class _BitNavListChild<TItem> where TItem : class
{
    private IDictionary<BitNavListItemAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavListItemAriaCurrent, string>()
    {
        [BitNavListItemAriaCurrent.Page] = "page",
        [BitNavListItemAriaCurrent.Step] = "step",
        [BitNavListItemAriaCurrent.Location] = "location",
        [BitNavListItemAriaCurrent.Time] = "time",
        [BitNavListItemAriaCurrent.Date] = "date",
        [BitNavListItemAriaCurrent.True] = "true"
    };

    [CascadingParameter] protected BitNavList<TItem> Parent { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    [Parameter] public TItem Item { get; set; } = default!;

    private string GetItemClasses(TItem item)
    {
        var enabledClass = Parent.GetIsEnabled(item) ? "enabled" : "disabled";
        var hasUrlClass = Parent.GetUrl(item).HasNoValue() ? "nourl" : "hasurl";

        var isSelected = item == Parent.SelectedItem ? "selected" : "";

        return $"link-{enabledClass}-{hasUrlClass} {isSelected}";
    }

    private static bool IsRelativeUrl(string url)
    {
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }
}
