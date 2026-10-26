namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.EmptyState;

public partial class BitAiEmptyStateDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Body",
            Type = "string?",
            DefaultValue = "null",
            Description = "The supporting line under the heading, in place of the EmptyBody of the string table (\"Ask a question to get started.\").",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered in place of the supporting line, which takes precedence over Body.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiEmptyStateClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the empty state.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the empty state, provided as custom CSS classes of an external icon library (or a ligature of Material Icons / Symbols, since it is drawn by a BitIcon). It wins over IconName; the library's stylesheet is the app's to load.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the empty state, from the built-in Fluent UI icons (BitIconName). Only two dozen of them ship in the core package's font; the rest need the Bit.BlazorUI.Icons package. Left unset (and with no Icon), a sparkle (AutoEnhanceOn) drawn from the AI package's own glyphs.",
            LinkType = LinkType.Link,
            Href = "/iconography",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template rendered in place of the icon, which takes precedence over Icon and IconName. Unlike the glyph, it is not hidden from assistive technologies: mark it aria-hidden when it only decorates.",
        },
        new()
        {
            Name = "OnSuggestion",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback for when a suggestion is chosen, with its text. Not raised while the empty state is disabled.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiEmptyStateClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the empty state.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Suggestions",
            Type = "IReadOnlyList<string>?",
            DefaultValue = "null",
            Description = "The example prompts offered as one-tap starters, each a button.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The heading, in place of the EmptyTitle of the string table (\"How can I help?\").",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiEmptyState-gap", DefaultValue = "spacing(1.5)", Description = "Room between the icon, the heading, the line and the suggestions." },
        new() { Name = "--bit-AiEmptyState-padding", DefaultValue = "spacing(6) spacing(2)", Description = "Inset of the empty state." },
        new() { Name = "--bit-AiEmptyState-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text of the supporting line." },
        new() { Name = "--bit-AiEmptyState-icon-color", DefaultValue = "var(--bit-clr-pri)", Description = "Color of the icon." },
        new() { Name = "--bit-AiEmptyState-icon-size", DefaultValue = "var(--bit-tpg-fs-4xl)", Description = "Size of the icon." },
        new() { Name = "--bit-AiEmptyState-title-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the heading." },
        new() { Name = "--bit-AiEmptyState-title-font-size", DefaultValue = "var(--bit-tpg-fs-xl)", Description = "Size of the heading." },
        new() { Name = "--bit-AiEmptyState-title-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the heading." },
        new() { Name = "--bit-AiEmptyState-suggestions-gap", DefaultValue = "spacing(1)", Description = "Room between two suggestions." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiEmptyStateClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiEmptyState."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitAiEmptyState."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the heading of the BitAiEmptyState."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the supporting line of the BitAiEmptyState."
                },
                new()
                {
                    Name = "Suggestions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the suggestions of the BitAiEmptyState."
                },
                new()
                {
                    Name = "Suggestion",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each suggestion button of the BitAiEmptyState.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Names a glyph for any icon set. A class-based set (Fabric MDL2, FontAwesome, Bootstrap Icons) is described by BaseClass, Prefix and Name; a ligature-based set (Material Icons, Material Symbols) puts the family on BaseClass and the ligature on Content. The static factories build each of them: Bit(name), Fa(icons), Bi(name), Mi(name, style), Ms(name, style), Css(cssClasses). A plain string converts implicitly and is taken as the complete class list.",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The name of the icon. For an external set this can be the complete CSS class list when BaseClass and Prefix are empty."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The base CSS class of the icon set - \"bit-icon\" for the built-in set, \"bi\" for Bootstrap Icons, \"material-symbols-outlined\" for Material Symbols. Leave it empty for a set that needs none."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The CSS class prefix written before the icon name - \"bit-icon--\" for the built-in set, \"bi-\" for Bootstrap Icons. Leave it empty for a set that uses none."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text rendered inside the icon element - the ligature of a ligature-based icon set such as Material Icons or Material Symbols. Class-based sets leave it null. The empty state draws its icon with a BitIcon, which renders it."
               },
               new()
               {
                   Name = "IsEmpty",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "Whether this instance names no glyph at all. An empty instance is treated as no icon, so an IconName given beside it is still used."
               },
            ]
        },
    ];



    private string? chosenSuggestion;
    private readonly string[] suggestions = ["Summarize this page", "Write a release note", "Explain render modes"];

    private void HandleSuggestion(string suggestion)
    {
        chosenSuggestion = suggestion;
    }


    private readonly BitAiEmptyStateParams[] emptyStateParams =
    [
        new()
        {
            Title = "Ask the docs",
            Body = "Both empty states take their words and glyph from the BitParams around them.",
            IconName = BitIconName.ChatBot,
        }
    ];


    private readonly string[] rtlSuggestions = ["این صفحه را خلاصه کن", "یک یادداشت انتشار بنویس"];
}
