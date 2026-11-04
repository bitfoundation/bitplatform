using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class ShareButtons
{
    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private Navigator navigator = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    [Parameter, EditorRequired] public string Url { get; set; } = default!;

    [Parameter] public string? Class { get; set; }

    private bool canShare;
    private bool copied;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            canShare = await navigator.CanShare(new ShareData { Title = Title, Url = Url });
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Share() => await navigator.Share(new ShareData { Title = Title, Url = Url });

    private async Task Copy()
    {
        await clipboard.WriteText(Url);
        copied = true;
    }

    private static string Encoded(string value) => Uri.EscapeDataString(value);
}
