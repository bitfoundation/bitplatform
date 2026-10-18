namespace Bit.Balette;

/// <summary>
/// A command declared in markup inside a <see cref="Balette"/> (directly or in a <see cref="BaletteGroup"/>), with a
/// parameter for every member of <see cref="BaletteCommand"/>. Nested items turn it into a page. It belongs to that
/// palette instance rather than to the service's registry, and re-registers only when one of its parameters changed.
/// </summary>
public partial class BaletteItem;
