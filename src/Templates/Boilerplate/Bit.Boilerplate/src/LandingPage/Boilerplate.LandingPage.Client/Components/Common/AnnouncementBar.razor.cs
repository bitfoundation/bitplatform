using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class AnnouncementBar
{
    [AutoInject] private LocalStorage localStorage = default!;

    [Parameter, EditorRequired] public string Key { get; set; } = default!;

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool isVisible = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && await localStorage.GetItem($"lp-announcement-{Key}") is not null)
        {
            isVisible = false;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Dismiss()
    {
        isVisible = false;
        await localStorage.SetItem($"lp-announcement-{Key}", "dismissed");
    }
}
