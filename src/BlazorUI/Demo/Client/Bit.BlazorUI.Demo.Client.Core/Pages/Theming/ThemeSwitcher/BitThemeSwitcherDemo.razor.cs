namespace Bit.BlazorUI.Demo.Client.Core.Pages.Theming.ThemeSwitcher;

public partial class BitThemeSwitcherDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitThemeSwitcherClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the switcher.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DarkSchemeIconName",
            Type = "string",
            DefaultValue = "ClearNight",
            Description = "The icon of the button that switches to the dark scheme - the one shown while the light scheme is active. A Fluent UI (Fabric MDL2) icon name, rendered through the Bit.BlazorUI.Icons stylesheet.",
        },
        new()
        {
            Name = "DarkSchemeTitle",
            Type = "string?",
            DefaultValue = "Turn off light",
            Description = "The title and aria-label of the button that switches to the dark scheme.",
        },
        new()
        {
            Name = "DesignSystems",
            Type = "IEnumerable<BitThemeSwitcherItem>?",
            DefaultValue = "null",
            Description = "The design systems offered by the picker. Defaults to the three that ship with the library (DefaultDesignSystems). The first item is also the fallback the picker shows for an applied theme that no item claims.",
            LinkType = LinkType.Link,
            Href = "#theme-switcher-item",
        },
        new()
        {
            Name = "DesignSystemTitle",
            Type = "string?",
            DefaultValue = "Design system",
            Description = "The title and aria-label of the design system picker.",
        },
        new()
        {
            Name = "InitialTheme",
            Type = "string?",
            DefaultValue = "null",
            Description = "The theme to reflect until the applied one can be read back, which takes JS interop and therefore a first interactive render. Hand it the theme the app persisted (the bit-theme-preference cookie, which the client mirrors the choice into when the host page opts in with the bit-theme-persist-cookie attribute) so prerendered markup shows the visitor's own design system instead of showing the first item until hydration.",
        },
        new()
        {
            Name = "LightSchemeIconName",
            Type = "string",
            DefaultValue = "Sunny",
            Description = "The icon of the button that switches to the light scheme - the one shown while the dark scheme is active. A Fluent UI (Fabric MDL2) icon name, rendered through the Bit.BlazorUI.Icons stylesheet.",
        },
        new()
        {
            Name = "LightSchemeTitle",
            Type = "string?",
            DefaultValue = "Turn on light",
            Description = "The title and aria-label of the button that switches to the light scheme.",
        },
        new()
        {
            Name = "NoColorScheme",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the light/dark toggle, leaving only the design system picker.",
        },
        new()
        {
            Name = "NoDesignSystem",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the design system picker, leaving only the light/dark toggle.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback that is called when the theme changes, receiving the applied theme name.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitThemeSwitcherClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the switcher.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "theme-switcher-item",
            Title = "BitThemeSwitcherItem",
            Description = "A design system offered by the BitThemeSwitcher: a name to show, and the two theme names its light and dark schemes are spelled with.",
            Parameters =
            [
                new()
                {
                    Name = "AriaLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The aria-label of this item in the picker. Falls back to Text.",
                },
                new()
                {
                    Name = "DarkTheme",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The theme name applied for this design system's dark scheme. Defaults to \"{Value}-dark\".",
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether this design system can be selected.",
                },
                new()
                {
                    Name = "LightTheme",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The theme name applied for this design system's light scheme. Defaults to \"{Value}-light\".",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text shown for this design system in the picker. Falls back to Value.",
                },
                new()
                {
                    Name = "Value",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The design system this item selects - the stem its two theme names share, e.g. \"material\" for the material-light / material-dark pair. It is also what identifies the item, so it has to be unique within one switcher, and it is matched against the applied theme name to decide which item is the selected one.",
                },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitThemeSwitcherClassStyles",
            Description = "Custom CSS classes/styles for different parts of the BitThemeSwitcher.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitThemeSwitcher.",
                },
                new()
                {
                    Name = "DesignSystem",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the design system picker of the BitThemeSwitcher.",
                },
                new()
                {
                    Name = "ColorSchemeButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for both color scheme buttons of the BitThemeSwitcher.",
                },
                new()
                {
                    Name = "DarkSchemeButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the color scheme button that is shown while the dark scheme is active (the one that switches to light) of the BitThemeSwitcher.",
                },
                new()
                {
                    Name = "LightSchemeButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the color scheme button that is shown while the light scheme is active (the one that switches to dark) of the BitThemeSwitcher.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of each color scheme button of the BitThemeSwitcher.",
                },
            ]
        },
    ];



    private string? changedTheme;

    private readonly List<BitThemeSwitcherItem> customDesignSystems =
    [
        new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
        new() { Text = "Cupertino", Value = BitExtraThemePresets.Cupertino },
    ];



    private readonly string example1RazorCode = @"
<BitThemeSwitcher />";

    private readonly string example2RazorCode = @"
<BitThemeSwitcher DesignSystems=""customDesignSystems"" />

@code {
    private readonly List<BitThemeSwitcherItem> customDesignSystems =
    [
        new() { Text = ""Fluent"", Value = ""fluent"", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
        new() { Text = ""Cupertino"", Value = BitExtraThemePresets.Cupertino },
    ];
}";

    private readonly string example3RazorCode = @"
<BitThemeSwitcher NoColorScheme />

<BitThemeSwitcher NoDesignSystem />";

    private readonly string example4RazorCode = @"
<BitThemeSwitcher DesignSystemTitle=""Skin""
                  DarkSchemeTitle=""Go dark""
                  LightSchemeTitle=""Go light""
                  DarkSchemeIconName=""@BitIconName.Brightness""
                  LightSchemeIconName=""@BitIconName.Sunny"" />";

    private readonly string example5RazorCode = @"
<BitThemeSwitcher OnChange=""t => changedTheme = t"" />

<div>Applied theme: @changedTheme</div>

@code {
    private string? changedTheme;
}";

    private readonly string example6RazorCode = @"
<style>
    .custom-scheme-btn {
        border-radius: 0.25rem;
    }
</style>

<BitThemeSwitcher Style=""padding:0.5rem;border:1px dashed gray;border-radius:0.5rem"" />

<BitThemeSwitcher Classes=""@(new() { ColorSchemeButton = ""custom-scheme-btn"" })"" />

<BitThemeSwitcher Styles=""@(new() { Root = ""gap:1.5rem"", ColorSchemeButton = ""border-radius:0.25rem"" })"" />";

    private readonly string example7RazorCode = @"
<BitThemeSwitcher Dir=""BitDir.Rtl"" />";
}
