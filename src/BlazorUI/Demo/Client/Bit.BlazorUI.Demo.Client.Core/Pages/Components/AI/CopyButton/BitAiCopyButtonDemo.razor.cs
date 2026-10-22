namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CopyButton;

public partial class BitAiCopyButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiCopyButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the copy button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ConfirmationDuration",
            Type = "TimeSpan",
            DefaultValue = "TimeSpan.FromSeconds(2)",
            Description = "How long the confirmation - the check mark and the Copied name - stays on the button after a copy. Another copy while it shows starts it over.",
        },
        new()
        {
            Name = "Content",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to copy, read when the button is pressed. A null value disables the button; an empty text is copied like any other.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the button: its tooltip, its accessible name, and its text when ShowLabel is set. While the confirmation lasts the Copied text of the strings in force takes its place. When null, the Copy text of the strings in force is used (BitAiStrings.Copy, \"Copy\").",
        },
        new()
        {
            Name = "OnCopied",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised after the text has been put on the clipboard - never for a copy the browser refused.",
        },
        new()
        {
            Name = "ShowLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the name of the button beside its icon.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiCopyButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the copy button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiCopyButtonClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiCopyButton."
                },
                new()
                {
                    Name = "Button",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton of the BitAiCopyButton.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "LiveRegion",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden live region of the BitAiCopyButton, which announces the copy."
                },
            ]
        },
    ];



    private const string cssVariablesStyle = "--bit-Button-text-color:var(--bit-clr-pri);" +
                                             "--bit-Button-text-hover-background:var(--bit-clr-pri-light);" +
                                             "--bit-Button-radius:2rem;";

    private int copyCount;

    private string? draft;

    private readonly BitAiCopyButtonParams[] copyButtonParams =
    [
        new() { ShowLabel = true, ConfirmationDuration = TimeSpan.FromSeconds(4) }
    ];

    private void HandleCopied()
    {
        copyCount++;
    }
}
