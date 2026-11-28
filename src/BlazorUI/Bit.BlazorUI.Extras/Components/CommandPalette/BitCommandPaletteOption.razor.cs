namespace Bit.BlazorUI;

/// <summary>
/// A command declared in markup inside a <see cref="BitCommandPalette"/> (directly or in a <see cref="BitCommandPaletteGroup"/>), with a
/// parameter for most members of <see cref="BitCommandPaletteCommand"/>; <c>Data</c>, <c>VisibleWhen</c>, <c>ExcludeFromHistory</c>,
/// <c>Actions</c>, <c>ForceLoad</c>, lazily loaded children, page sources and handlers that answer with a prompt, a page
/// or a toast need <see cref="BitCommandPaletteCmd"/>. Nested items turn it into a page. It belongs to that palette instance rather
/// than to the service's registry, and re-registers only when one of its parameters changed.
/// </summary>
public partial class BitCommandPaletteOption;
