namespace Bit.BlazorUI;

/// <summary>
/// The <c>bit-theme</c> attribute values of the design-system presets that ship with this package, as
/// <c>const</c>s. <see cref="BitThemePresets"/> carries the same nine names as extension members, and
/// that is the form to reach for - <c>BitThemePresets.MaterialDark</c> next to
/// <c>BitThemePresets.FluentDark</c>, with no package-specific table to know about. This type is what
/// the extension members are built from, and it stays public for the two places they cannot go.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to name this type instead.</b> Extension members are a C# 14 feature on BOTH sides: a
/// consumer compiling at an older language version - which is the default for a <c>net8.0</c> or
/// <c>net9.0</c> app, the two other frameworks this package targets - cannot even read one
/// (<c>error CS9202: Feature 'extensions' is not available in C# 12.0</c>). And because an extension
/// member is a property rather than a <c>const</c>, it cannot be a <c>case</c> label, an attribute
/// argument or a default parameter value at any language version. Both cases read the token from
/// here.
/// </para>
/// <para>
/// Unlike the Fluent presets, these are not self-contained in <c>bit.blazorui.css</c>: each is an
/// override-only bundle of <c>--bit-*</c> tokens that must be linked AFTER the core stylesheet -
/// <c>_content/Bit.BlazorUI.Extras/styles/bit.blazorui.material.css</c>,
/// <c>_content/Bit.BlazorUI.Extras/styles/bit.blazorui.cupertino.css</c> or
/// <c>_content/Bit.BlazorUI.Extras/styles/bit.blazorui.fluent2.css</c>. Setting one of these names
/// without linking its bundle leaves the app on the Fluent defaults, because there is then nothing
/// for the attribute to select.
/// </para>
/// <para>
/// A <c>const</c> is inlined by the compiler, so - unlike the extension members, which are real reads
/// of this assembly - naming one here does NOT load the package and therefore does not register its
/// first-paint surfaces with <see cref="BitThemePresetRegistry"/>. An app whose only other use of the
/// package is its stylesheet calls <see cref="BitExtraThemeRegistration.Register"/> once, or reads
/// the surfaces through <see cref="BitExtraThemeSurfaces"/>, which registers them on the way.
/// </para>
/// </remarks>
public static class BitExtraThemePresets
{
    /// <summary>Fluent 2 base preset; follows the light palette.</summary>
    public const string Fluent2 = "fluent2";

    /// <summary>Fluent 2 light preset.</summary>
    public const string Fluent2Light = "fluent2-light";

    /// <summary>Fluent 2 dark preset.</summary>
    public const string Fluent2Dark = "fluent2-dark";

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
