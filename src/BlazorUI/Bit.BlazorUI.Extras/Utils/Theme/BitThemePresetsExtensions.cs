namespace Bit.BlazorUI;

/// <summary>
/// The <c>bit-theme</c> attribute values of the design-system presets that ship with this package,
/// added to <see cref="BitThemePresets"/> itself so they are reached exactly like the presets the core
/// stylesheet carries - <c>BitThemePresets.MaterialDark</c> next to <c>BitThemePresets.FluentDark</c>,
/// with no second table to know about.
/// </summary>
/// <remarks>
/// <para>
/// This container is never named in code: the members below are extension members on
/// <see cref="BitThemePresets"/>, and <c>Bit.BlazorUI</c> being in scope is all it takes to see them.
/// </para>
/// <para>
/// <b>Two things they cannot do, and what to write instead.</b> They are static properties rather
/// than <c>const</c>s, so one cannot be a <c>case</c> label, an attribute argument or a default
/// parameter value; and extension members are a C# 14 feature on the READING side too, so a consumer
/// compiling at an older language version - the default for a <c>net8.0</c> or <c>net9.0</c> app,
/// which this package still targets - cannot read one at all
/// (<c>error CS9202: Feature 'extensions' is not available in C# 12.0</c>). Both cases name
/// <see cref="BitExtraThemePresets"/>, whose <c>const</c>s these members return, and which is public
/// for exactly that.
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
/// Reading any of these is also what registers this package's presets with
/// <see cref="BitThemePresetRegistry"/>, and so what puts their first-paint surfaces into
/// <see cref="BitThemeSurfaces"/>: the read loads this assembly, and loading it runs
/// <see cref="BitExtraThemeRegistration"/>. Naming a preset here rather than writing its token as a
/// bare string is what keeps the two in step.
/// </para>
/// </remarks>
public static class BitThemePresetsExtensions
{
    extension(BitThemePresets)
    {
        /// <summary>Fluent 2 base preset (<c>"fluent2"</c>); follows the light palette. Requires <c>bit.blazorui.fluent2.css</c>.</summary>
        public static string Fluent2 => BitExtraThemePresets.Fluent2;

        /// <summary>Fluent 2 light preset (<c>"fluent2-light"</c>).</summary>
        public static string Fluent2Light => BitExtraThemePresets.Fluent2Light;

        /// <summary>Fluent 2 dark preset (<c>"fluent2-dark"</c>).</summary>
        public static string Fluent2Dark => BitExtraThemePresets.Fluent2Dark;

        /// <summary>Material base preset (<c>"material"</c>); follows the light palette. Requires <c>bit.blazorui.material.css</c>.</summary>
        public static string Material => BitExtraThemePresets.Material;

        /// <summary>Material light preset (<c>"material-light"</c>).</summary>
        public static string MaterialLight => BitExtraThemePresets.MaterialLight;

        /// <summary>Material dark preset (<c>"material-dark"</c>).</summary>
        public static string MaterialDark => BitExtraThemePresets.MaterialDark;

        /// <summary>Cupertino base preset (<c>"cupertino"</c>); follows the light palette. Requires <c>bit.blazorui.cupertino.css</c>.</summary>
        public static string Cupertino => BitExtraThemePresets.Cupertino;

        /// <summary>Cupertino light preset (<c>"cupertino-light"</c>).</summary>
        public static string CupertinoLight => BitExtraThemePresets.CupertinoLight;

        /// <summary>Cupertino dark preset (<c>"cupertino-dark"</c>).</summary>
        public static string CupertinoDark => BitExtraThemePresets.CupertinoDark;
    }
}
