namespace Bit.BlazorUI;

/// <summary>
/// The <c>bit-theme</c> attribute values of the design-system presets that ship with this package, alongside
/// the <see cref="BitThemePresets"/> Fluent presets that are built into the core stylesheet.
/// </summary>
/// <remarks>
/// Unlike the Fluent presets, these are not self-contained in <c>bit.blazorui.css</c>: each is an
/// override-only bundle of <c>--bit-*</c> tokens that must be linked AFTER the core stylesheet -
/// <c>_content/Bit.BlazorUI.Extras/styles/bit.blazorui.material.css</c> or
/// <c>_content/Bit.BlazorUI.Extras/styles/bit.blazorui.cupertino.css</c>. Setting one of these names without
/// linking its bundle leaves the app on the Fluent defaults, because there is then nothing for the attribute
/// to select.
/// </remarks>
public static class BitExtraThemePresets
{
    /// <summary>Material base preset; follows the light palette.</summary>
    public const string Material = "material";

    /// <summary>Material light preset.</summary>
    public const string MaterialLight = "material-light";

    /// <summary>Material dark preset.</summary>
    public const string MaterialDark = "material-dark";

    /// <summary>Cupertino base preset; follows the light palette.</summary>
    public const string Cupertino = "cupertino";

    /// <summary>Cupertino light preset.</summary>
    public const string CupertinoLight = "cupertino-light";

    /// <summary>Cupertino dark preset.</summary>
    public const string CupertinoDark = "cupertino-dark";
}
