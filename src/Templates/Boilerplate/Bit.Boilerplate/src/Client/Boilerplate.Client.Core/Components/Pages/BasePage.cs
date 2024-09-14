
namespace Boilerplate.Client.Core.Components.Pages;

public abstract partial class BasePage : AppComponentBase
{
    protected abstract string? Title { get; }
    protected abstract string? Subtitle { get; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        PubSubService.Publish(PubSubMessages.PAGE_TITLE_CHANGED, (Title, Subtitle));
    }
}
