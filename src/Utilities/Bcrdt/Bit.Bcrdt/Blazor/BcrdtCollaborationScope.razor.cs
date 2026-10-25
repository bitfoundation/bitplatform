namespace Bit.Bcrdt.Blazor;

/// <summary>
/// Opens a collaborative document for its content and closes it again: renders <see cref="ChildContent"/> with the
/// open <see cref="BcrdtCollaborativeDocument"/> as its context (and cascades it), shows <see cref="LoadingContent"/>
/// while it opens, re-renders on every change, and follows <see cref="DocumentId"/> when it changes. Requires
/// <c>AddBitBcrdtServices</c>.
/// </summary>
public partial class BcrdtCollaborationScope
{
#if NET9_0_OR_GREATER
    private bool IsInteractive => RendererInfo.IsInteractive;
#else
    // .NET 8 cannot tell a prerender from an interactive render while parameters are set, but only an
    // interactive render is ever followed by OnAfterRender: the first one marks the component interactive
    // and opens the document.
    private bool IsInteractive { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _isDisposed) return;

        IsInteractive = true;
        await FollowDocumentIdAsync();

        // Nothing renders after OnAfterRenderAsync by itself, as it does after OnParametersSetAsync.
        if (!_isDisposed) StateHasChanged();
    }
#endif
}
