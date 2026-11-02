namespace Bit.Balette;

/// <summary>
/// A command declared in markup inside a <see cref="Balette"/> (directly or in a <see cref="BaletteGroup"/>), with a
/// parameter for most members of <see cref="BaletteCommand"/>; <c>Data</c>, <c>VisibleWhen</c>, <c>ExcludeFromHistory</c>,
/// <c>Actions</c>, <c>ForceLoad</c>, lazily loaded children, page sources and handlers that answer with a prompt, a page
/// or a toast need <see cref="BaletteCmd"/>. Nested items turn it into a page. It belongs to that palette instance rather
/// than to the service's registry, and re-registers only when one of its parameters changed.
/// </summary>
public partial class BaletteItem;
