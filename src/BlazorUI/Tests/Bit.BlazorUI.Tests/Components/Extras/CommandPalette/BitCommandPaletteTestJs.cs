namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// The script function the palette starts its browser layer through (BitCommandPaletteJsRuntimeExtensions), which answers
/// with the handle the palette then calls by its members (open, close, focusInput, isMac, dispose, ...). A bUnit test sets
/// it up with <c>Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, ...)</c>.
/// </summary>
internal static class BitCommandPaletteTestJs
{
    public const string Setup = "BitBlazorUI.BitCommandPalette.setup";
}
