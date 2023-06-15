using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Theme;
internal static class BitThemeJsExtensions
{
    internal static async Task ChangeTheme(this IJSRuntime jsRuntime, string themeName)
    {
        await jsRuntime.InvokeVoidAsync("BitTheme.changeTheme", themeName);
    }
    internal static async Task ApplyBitTheme(this IJSRuntime jsRuntime, Dictionary<string, string> theme, ElementReference? element)
    {
        await jsRuntime.InvokeVoidAsync("BitTheme.applyBitTheme", theme, element);
    }
}
