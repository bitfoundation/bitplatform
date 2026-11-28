namespace Bit.BlazorUI;

/// <summary>
/// Registers <see cref="Commands"/> with <see cref="IBitCommandPaletteService"/> while it is rendered and removes them when it is
/// disposed - page-scoped commands, like kbar's <c>useRegisterActions</c>. Hand it a new collection instance to swap the
/// registration, for example after a toggle's state changed.
/// </summary>
public partial class BitCommandPaletteCommands;
