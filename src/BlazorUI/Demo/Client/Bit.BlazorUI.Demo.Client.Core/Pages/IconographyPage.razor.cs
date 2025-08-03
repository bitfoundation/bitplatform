using System.Reflection;

namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class IconographyPage
{
    private List<string> allIcons = default!;
    private List<string> filteredIcons = default!;

    protected override void OnInitialized()
    {
        allIcons = typeof(BitIconName).GetFields(BindingFlags.Static | BindingFlags.Public)
                                      .Select(m => m.GetValue(null)?.ToString()!)
                                      .ToList();
        HandleClear();
        base.OnInitialized();
    }

    private void HandleClear()
    {
        filteredIcons = allIcons;
    }

    private void HandleChange(string text)
    {
        HandleClear();
        if (string.IsNullOrEmpty(text)) return;

        filteredIcons = allIcons.FindAll(icon => string.IsNullOrEmpty(icon) is false && icon.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }
}
