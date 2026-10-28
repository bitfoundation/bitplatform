namespace Bit.Bcrdt.Blazor;

/// <summary>
/// Opens a collaborative document for its content and closes it again: renders <see cref="ChildContent"/> with the
/// open <see cref="BcrdtCollaborativeDocument"/> as its context (and cascades it), shows <see cref="LoadingContent"/>
/// while it opens, re-renders on every change, and follows <see cref="DocumentId"/> when it changes. Opens it through
/// the service <c>AddBitBcrdtServices</c> registered - or, in an app that lazy loads Bit.Bcrdt on WebAssembly, the one
/// <see cref="BitBcrdt.GetBcrdtCollaborationService"/> configured.
/// </summary>
public partial class BcrdtCollaborationScope
{
    private bool IsInteractive => RendererInfo.IsInteractive;
}
