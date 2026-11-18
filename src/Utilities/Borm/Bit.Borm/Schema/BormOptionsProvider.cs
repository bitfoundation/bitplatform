namespace Bit.Borm;

/// <summary>
/// Loads the choices for a select/radio/multi-select field on demand. This is what makes cascading
/// selects possible — "cities" reading the currently selected "country" — and lets options come from a
/// service instead of being baked into the schema.
/// </summary>
public delegate ValueTask<IReadOnlyList<BormSelectOption>> BormOptionsProvider(BormOptionsContext context);
