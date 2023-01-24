
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.NavGroup;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavGroupChild
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    private Dictionary<BitNavOptionAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavOptionAriaCurrent, string>()
    {
        [BitNavOptionAriaCurrent.Page] = "page",
        [BitNavOptionAriaCurrent.Step] = "step",
        [BitNavOptionAriaCurrent.Location] = "location",
        [BitNavOptionAriaCurrent.Time] = "time",
        [BitNavOptionAriaCurrent.Date] = "date",
        [BitNavOptionAriaCurrent.True] = "true"
    };

    [CascadingParameter] protected BitNavGroup Parent { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    [Parameter] public BitNavOption Option { get; set; } = default!;

    private string GetOptionClasses(BitNavOption option)
    {
        var enabledClass = option.IsEnabled ? "enabled" : "disabled";
        var hasUrlClass = option.Url.HasNoValue() ? "nourl" : "hasurl";

        var isSelected = option.Key == Parent.SelectedKey ? "selected" : "";

        return $"link-{enabledClass}-{hasUrlClass} {isSelected}";
    }

    private static bool IsRelativeUrl(string url)
    {
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }
}
