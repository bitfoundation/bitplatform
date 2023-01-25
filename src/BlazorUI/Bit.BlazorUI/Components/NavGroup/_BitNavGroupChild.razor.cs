using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Components.NavGroup;

#pragma warning disable CA1707 // Identifiers should not contain underscores
public partial class _BitNavGroupChild
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    private Dictionary<BitNavItemAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavItemAriaCurrent, string>()
    {
        [BitNavItemAriaCurrent.Page] = "page",
        [BitNavItemAriaCurrent.Step] = "step",
        [BitNavItemAriaCurrent.Location] = "location",
        [BitNavItemAriaCurrent.Time] = "time",
        [BitNavItemAriaCurrent.Date] = "date",
        [BitNavItemAriaCurrent.True] = "true"
    };

    [CascadingParameter] protected BitNavGroup Parent { get; set; } = default!;

    [Parameter] public int Depth { get; set; }

    [Parameter] public BitNavOption Option { get; set; } = default!;

    private string GetOptionClasses()
    {
        var enabledClass = Option.IsEnabled is false ? "disabled" : "";

        var isSelected = Option.Key == Parent.SelectedKey ? "selected" : "";

        var isHeader = Parent.RenderType == BitNavRenderType.Grouped && Parent._options.Any(o => o == Option) ? "group-header" : "";

        return $"{enabledClass} {isSelected} {isHeader}";
    }

    private static bool IsRelativeUrl(string url)
    {
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }
}
