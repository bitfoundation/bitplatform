using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil06KeyboardPage
{
    private BitSearchBox searchBox = default!;


    protected override async Task OnInitAsync()
    {
        await keyboard.Add(ButilKeyCodes.KeyF, () => _ = searchBox.FocusInput(), ButilModifiers.Ctrl);
    }


    private string addExampleCode =
@"@inject Bit.Butil.Document document

<div>Press Ctrl+F to focus on search box</div>

<BitSearchBox @ref=""searchBox"" />

@code {
    private BitSearchBox searchBox = default!;

    protected override async Task OnInitAsync()
    {
        await keyboard.Add(ButilKeyCodes.KeyF, () => _ = searchBox.FocusInput(), ButilModifiers.Ctrl);
    }
}";
}
