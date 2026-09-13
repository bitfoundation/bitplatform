namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppFileSaveAnchor
{
    private ElementReference anchorRef;

    [AutoInject] private FileSaveService fileSaveService = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        fileSaveService.Anchor = anchorRef;
    }
}
