namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>What the Theme page's sections share: the forced states, the color roles and the date its samples are built around.</summary>
public static class ThemeSamples
{
    /// <summary>The states shown for interactive components, with the state Scripts/states.ts forces; null is the untouched default.</summary>
    public static readonly (string NameKey, string? Force)[] InteractionStates =
    [
        (nameof(WorkbenchStrings.ThemeStateDefault), null),
        (nameof(WorkbenchStrings.ThemeStateHover), "hover"),
        (nameof(WorkbenchStrings.ThemeStateFocused), "focus"),
        (nameof(WorkbenchStrings.ThemeStatePressed), "active")
    ];

    /// <summary>The eight color roles a component's Color parameter takes.</summary>
    public static readonly BitColor[] Colors = [BitColor.Primary, BitColor.Secondary, BitColor.Tertiary, BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    /// <summary>The roles that tell how something went.</summary>
    public static readonly BitColor[] StatusColors = [BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    /// <summary>The day the samples' dates are built around, so every run and every screenshot shows the same ones.</summary>
    public static readonly DateTimeOffset Today = new(2026, 5, 18, 9, 30, 0, TimeSpan.Zero);

    /// <summary>A wide illustration the app ships, for the samples that need a picture larger than an avatar.</summary>
    public const string IllustrationUrl = "_content/Boilerplate.Client.Core/images/identitylayout-image.webp";
}
