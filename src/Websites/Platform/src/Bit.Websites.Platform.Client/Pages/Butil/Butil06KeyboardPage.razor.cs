using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil06KeyboardPage
{
    private BitSearchBox searchBox = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await keyboard.Add(ButilKeyCodes.KeyF, () => _ = searchBox?.FocusAsync(), ButilModifiers.Ctrl);
    }


    private string addExampleCode =
@"@inject Bit.Butil.Keyboard keyboard

<div>Press Ctrl+F to focus on search box</div>

<BitSearchBox @ref=""searchBox"" />

@code {
    private BitSearchBox searchBox = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await keyboard.Add(ButilKeyCodes.KeyF, () => _ = searchBox?.FocusAsync(), ButilModifiers.Ctrl);
    }
}";
}
