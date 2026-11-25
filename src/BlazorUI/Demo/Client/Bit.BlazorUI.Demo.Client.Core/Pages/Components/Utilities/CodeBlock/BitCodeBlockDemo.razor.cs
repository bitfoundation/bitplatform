namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CodeBlock;

public partial class BitCodeBlockDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The code as markup, rendered in place of Code: code written right inside the tag, or code an app highlighted itself. The copy puts Code on the clipboard where it is set, and otherwise the text the browser renders of this content.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCodeBlockClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the code block.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Code",
            Type = "string?",
            DefaultValue = "null",
            Description = "The code to show, which is also exactly what the copy puts on the clipboard.",
        },
        new()
        {
            Name = "CopiedText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the copy button while what it copied is on the clipboard, which is also what screen readers are told after a copy. Defaults to BitStrings.Copied (\"Copied\").",
        },
        new()
        {
            Name = "CopyOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Copies the code when the code itself is clicked, as well as through the copy button. A click that ends a selection of a part of the code copies nothing, and neither does a double or a triple click selecting a word or a line. It is a shortcut for the pointer only, so the copy button is best kept shown beside it.",
        },
        new()
        {
            Name = "CopyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the copy button, which is what a screen reader reads out for it and what the pointer shows as its tooltip. Defaults to BitStrings.Copy (\"Copy\").",
        },
        new()
        {
            Name = "Language",
            Type = "string?",
            DefaultValue = "null",
            Description = "The language of the code, shown as a label in the header. It is a label only: the code is not highlighted.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height (any CSS length) past which the code scrolls.",
        },
        new()
        {
            Name = "OnCopy",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback raised after the code has been put on the clipboard.",
        },
        new()
        {
            Name = "OnCopyFailed",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback raised when the browser refuses to put the code on the clipboard - a copy that comes too late after the press of the user, as one made after a round trip to the server can in Safari.",
        },
        new()
        {
            Name = "ShowCopyButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the button that copies the code.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCodeBlockClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the code block.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Terminal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the code block as a terminal window: a dark surface in both color schemes, the three dots of a title bar, and a prompt before every line of Code. Content handed in through ChildContent is drawn as it is written, with no prompt.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title shown in the header of the code block - a file name, most of the time - which also names the code to a screen reader.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCodeBlockClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitCodeBlock." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header bar of the BitCodeBlock, which holds the title, the language and the copy button (rendered when a Title or a Language is set, or the block is a Terminal)." },
                new() { Name = "Dots", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the three window dots of the header of a Terminal BitCodeBlock." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title in the header of the BitCodeBlock." },
                new() { Name = "Language", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the language label in the header of the BitCodeBlock." },
                new() { Name = "CopyButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the copy button of the BitCodeBlock." },
                new() { Name = "Body", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of the code of the BitCodeBlock." },
                new() { Name = "Pre", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the pre element of the BitCodeBlock." },
                new() { Name = "Code", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the code element of the BitCodeBlock." },
                new() { Name = "LiveRegion", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the visually hidden live region of the BitCodeBlock that announces a copy." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-CodeBlock-background", DefaultValue = "4% of --bit-clr-fg-pri", Description = "Background of the block. The Terminal parameter wins over it." },
        new() { Name = "--bit-CodeBlock-color", DefaultValue = "inherit", Description = "Text color of the code. The Terminal parameter wins over it." },
        new() { Name = "--bit-CodeBlock-border-color", DefaultValue = "--bit-clr-brd-sec", Description = "Border of the block and under its header. The Terminal parameter wins over it." },
        new() { Name = "--bit-CodeBlock-radius", DefaultValue = "--bit-shp-radius-surface", Description = "Corner radius of the block." },
        new() { Name = "--bit-CodeBlock-padding", DefaultValue = "spacing(1.5)", Description = "Padding around the code." },
        new() { Name = "--bit-CodeBlock-font-family", DefaultValue = "--bit-tpg-font-family-mono", Description = "Font of the code and of the language label." },
        new() { Name = "--bit-CodeBlock-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Text size of the code." },
        new() { Name = "--bit-CodeBlock-max-height", DefaultValue = "none", Description = "Height past which the code scrolls. The MaxHeight parameter wins over it." },
        new() { Name = "--bit-CodeBlock-header-background", DefaultValue = "4% of --bit-clr-fg-pri", Description = "Background of the header bar." },
        new() { Name = "--bit-CodeBlock-header-color", DefaultValue = "--bit-clr-fg-sec", Description = "Text color of the header bar." },
        new() { Name = "--bit-CodeBlock-copy-button-color", DefaultValue = "inherit", Description = "Color of the glyph of the copy button." },
        new() { Name = "--bit-CodeBlock-copy-button-hover-background", DefaultValue = "12% of the button's color", Description = "Background of the copy button under the pointer (20% while it is pressed)." },
        new() { Name = "--bit-CodeBlock-terminal-background", DefaultValue = "--bit-clr-ntr-gray190", Description = "Background of a Terminal block, dark in both color schemes." },
        new() { Name = "--bit-CodeBlock-terminal-color", DefaultValue = "--bit-clr-ntr-gray20", Description = "Text color of a Terminal block." },
        new() { Name = "--bit-CodeBlock-terminal-border-color", DefaultValue = "16% of --bit-clr-ntr-gray20", Description = "Border of a Terminal block." },
        new() { Name = "--bit-CodeBlock-prompt", DefaultValue = "\"$ \"", Description = "The prompt drawn before each line of a Terminal block, as a CSS string. It is neither selected nor copied with the commands." },
        new() { Name = "--bit-CodeBlock-prompt-color", DefaultValue = "--bit-clr-ntr-gray90", Description = "Color of the prompt of a Terminal block." },
    ];



    private int copyCount;

    private readonly string basicCode = "dotnet new install Bit.Boilerplate";

    private readonly string headerCode = """
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBitBlazorUIServices();

var app = builder.Build();
app.Run();
""";

    private readonly string jsonCode = """{ "name": "bit BlazorUI", "free": true }""";

    private readonly string terminalCode = """
dotnet tool install --global Bit.Cli
bit new MyFirstProject
""";

    private readonly string powershellCode = """
Set-Location .\MyFirstProject
dotnet run
""";

    private readonly string longCode = """
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "SqliteConnectionString": "Data Source=App_Data/AppDb.db;Mode=ReadWriteCreate;Cache=Shared"
  },
  "Identity": {
    "Issuer": "MyFirstProject",
    "Audience": "MyFirstProject",
    "BearerTokenExpiration": "0.00:05:00",
    "RefreshTokenExpiration": "14.00:00:00"
  }
}
""";

    private readonly string highlightedCode = "var greeting = \"Hello, world!\";";
}
