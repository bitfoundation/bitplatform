namespace Bit.Bcrdt.Blazor;

/// <summary>
/// Opens a collaborative document for its content and closes it again: renders <see cref="ChildContent"/> with the
/// open <see cref="BcrdtCollaborativeDocument"/> as its context (and cascades it), shows <see cref="LoadingContent"/>
/// while it opens, re-renders on every change, and follows <see cref="DocumentId"/> when it changes. Requires
/// <c>AddBitBcrdtServices</c>.
/// </summary>
public partial class BcrdtCollaborationScope
{
}
