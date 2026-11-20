namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CopyButton;

public partial class BitCopyButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitCopyButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the copy button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the button. Left unset, the button is primary unless the --bit-Button-* variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "CopiedDuration",
            Type = "TimeSpan",
            DefaultValue = "TimeSpan.FromSeconds(2)",
            Description = "How long the copied state - the copied glyph and text - stays on the button after a copy, before it goes back to what it was. Another copy while it shows starts it over.",
        },
        new()
        {
            Name = "CopiedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon the button shows while it is in the copied state, using custom CSS classes for external icon libraries. Takes precedence over CopiedIconName when both are set. With neither set, the button shows a check mark (Accept).",
        },
        new()
        {
            Name = "CopiedIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon the button shows while it is in the copied state. With neither this nor CopiedIcon set, the button shows a check mark (Accept).",
        },
        new()
        {
            Name = "CopiedText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the button carries, and the live region announces, while it is in the copied state. When null, the Copied text of the strings in force is used (BitStrings.Copied, \"Copied\").",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the button, using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set. With neither set, the button shows the Copy glyph.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon of the button. With neither this nor Icon set, the button shows the Copy glyph.",
        },
        new()
        {
            Name = "OnCopy",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback raised once the value is on the clipboard, with the value that was copied.",
        },
        new()
        {
            Name = "OnCopyFailed",
            Type = "EventCallback<Exception>",
            DefaultValue = "",
            Description = "The callback raised when the browser could not put the value on the clipboard (a permission denied, a copy command refused), with the error it gave. The button stays as it was.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the button. Left unset, the button is medium unless the --bit-Button-* variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCopyButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the copy button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the button; CopiedText takes its place while the button is in the copied state. When null, the button is an icon button named by the Copy text of the strings in force (BitStrings.Copy, \"Copy\") - or by AriaLabel - which is also its tooltip.",
        },
        new()
        {
            Name = "Value",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to put on the clipboard, read when the button is pressed. A null value disables the button; an empty text is copied like any other.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button. Left unset, the button is Fill.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCopyButtonClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCopyButton."
                },
                new()
                {
                    Name = "Button",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton of the BitCopyButton.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "LiveRegion",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden live region of the BitCopyButton, which announces the copy."
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitVariant(),
    ];



    private const string cssVariablesStyle = "--bit-Button-fill-background:var(--bit-clr-ter);" +
                                             "--bit-Button-fill-border-color:var(--bit-clr-ter);" +
                                             "--bit-Button-radius:2rem;";

    private int copyCount;
    private string? copyError;
    private string? draft;

    private readonly BitCopyButtonParams[] copyButtonParams =
    [
        new() { Variant = BitVariant.Outline, Text = "Copy", CopiedText = "Done", CopiedDuration = TimeSpan.FromSeconds(4) }
    ];

    private void HandleCopy(string value)
    {
        copyCount++;
        copyError = null;
    }

    private void HandleCopyFailed(Exception exception)
    {
        copyError = exception.Message;
    }
}
