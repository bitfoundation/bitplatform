
namespace Boilerplate.Client.Core.Components.Pages;

public abstract partial class AppPageBase : AppComponentBase
{
    protected virtual string? Title { get; }
    protected virtual string? Subtitle { get; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        PubSubService.Publish(PubSubMessages.PAGE_TITLE_CHANGED, (Title, Subtitle));
    }
}
