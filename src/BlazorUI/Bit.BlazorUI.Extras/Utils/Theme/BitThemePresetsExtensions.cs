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
/// They are static properties rather than <c>const</c>s, which is the one difference a consumer
/// notices - a property cannot be a <c>case</c> label, an attribute argument or a default parameter
/// value. Use <c>if</c>/<c>==</c> where a <c>switch</c> over constants was.
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
        public static string Fluent2 => BitExtraThemeTokens.Fluent2;

        /// <summary>Fluent 2 light preset (<c>"fluent2-light"</c>).</summary>
        public static string Fluent2Light => BitExtraThemeTokens.Fluent2Light;

        /// <summary>Fluent 2 dark preset (<c>"fluent2-dark"</c>).</summary>
        public static string Fluent2Dark => BitExtraThemeTokens.Fluent2Dark;

        /// <summary>Material base preset (<c>"material"</c>); follows the light palette. Requires <c>bit.blazorui.material.css</c>.</summary>
        public static string Material => BitExtraThemeTokens.Material;

        /// <summary>Material light preset (<c>"material-light"</c>).</summary>
        public static string MaterialLight => BitExtraThemeTokens.MaterialLight;

        /// <summary>Material dark preset (<c>"material-dark"</c>).</summary>
        public static string MaterialDark => BitExtraThemeTokens.MaterialDark;

        /// <summary>Cupertino base preset (<c>"cupertino"</c>); follows the light palette. Requires <c>bit.blazorui.cupertino.css</c>.</summary>
        public static string Cupertino => BitExtraThemeTokens.Cupertino;

        /// <summary>Cupertino light preset (<c>"cupertino-light"</c>).</summary>
        public static string CupertinoLight => BitExtraThemeTokens.CupertinoLight;

        /// <summary>Cupertino dark preset (<c>"cupertino-dark"</c>).</summary>
        public static string CupertinoDark => BitExtraThemeTokens.CupertinoDark;
    }
}

/// <summary>
/// The raw tokens behind this package's presets, as <c>const</c>s, for the few places inside the
/// package that need a compile-time constant (an attribute argument, a <c>case</c> label) and for the
/// registration below. Everything outside reads them through <see cref="BitThemePresets"/>.
/// </summary>
internal static class BitExtraThemeTokens
{
    internal const string Fluent2 = "fluent2";
    internal const string Fluent2Light = "fluent2-light";
    internal const string Fluent2Dark = "fluent2-dark";
    internal const string Material = "material";
    internal const string MaterialLight = "material-light";
    internal const string MaterialDark = "material-dark";
    internal const string Cupertino = "cupertino";
    internal const string CupertinoLight = "cupertino-light";
    internal const string CupertinoDark = "cupertino-dark";
}
