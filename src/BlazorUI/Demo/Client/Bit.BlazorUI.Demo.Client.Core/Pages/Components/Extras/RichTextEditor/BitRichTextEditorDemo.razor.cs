namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.RichTextEditor;

public partial class BitRichTextEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitRichTextEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the rich text editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "EditorTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the editor content."
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder value of the editor."
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the editor readonly."
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the location of the Toolbar and the Editor."
        },
        new()
        {
            Name = "Styles",
            Type = "BitRichTextEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the rich text editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Theme",
            Type = "BitRichTextEditorTheme?",
            DefaultValue = "null",
            Description = "The theme of the editor.",
            Href = "#rich-text-editor-theme",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ToolbarTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the toolbar content."
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "GetText",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current text content of the editor."
        },
        new()
        {
            Name = "GetHtml",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current html content of the editor."
        },
        new()
        {
            Name = "GetContent",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current content of the editor in JSON format."
        },
        new()
        {
            Name = "SetText",
            Type = "Action<ValueTask<string?>>",
            Description = "Sets the current text content of the editor."
        },
        new()
        {
            Name = "SetHtml",
            Type = "Action<ValueTask<string?>>",
            Description = "Sets the current html content of the editor."
        },
        new()
        {
            Name = "SetContent",
            Type = "Action<ValueTask<string?>>",
            Description = "Sets the current content of the editor in JSON format."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitRichTextEditorClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root of the BitRichTextEditor.",
                },
                new()
                {
                    Name = "Toolbar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar of the BitRichTextEditor.",
                },
                new()
                {
                    Name = "Editor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the editor container of the BitRichTextEditor.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "rich-text-editor-theme",
            Name = "BitRichTextEditorTheme",
            Description = "",
            Items =
            [
                new() { Name = "Snow", Value = "0" },
                new() { Name = "Bubble", Value = "1" },
            ]
        }
    ];


    private BitRichTextEditor getEditorRef = default!;
    private string? result;
    private async Task GetText()
    {
        result = await getEditorRef.GetText();
    }
    private async Task GetHtml()
    {
        result = await getEditorRef.GetHtml();
    }
    private async Task GetContent()
    {
        result = await getEditorRef.GetContent();
    }

    private BitRichTextEditor setEditorRef = default!;
    private string? setValue;
    private async Task SetText()
    {
        await setEditorRef.SetText(setValue);
    }
    private async Task SetHtml()
    {
        await setEditorRef.SetHtml(setValue);
    }
    private async Task SetContent()
    {
        await setEditorRef.SetContent(setValue);
    }



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";

    private readonly string example2RazorCode = @"
<BitRichTextEditor Placeholder=""This is a custom placeholder"" />";

    private readonly string example3RazorCode = @"
<BitRichTextEditor Placeholder=""This is a custom placeholder"" />";

    private readonly string example4RazorCode = @"
<BitRichTextEditor Placeholder=""The toolbar location is reversed!"" Reversed />";

    private readonly string example5RazorCode = @"
<BitRichTextEditor Style=""min-height: 300px"" FullToolbar />";

    private readonly string example6RazorCode = @"
<BitRichTextEditor Styles=""@(new() { Toolbar = ""border-color: red"", Editor = ""border-color: blue""})"" />";

    private readonly string example7RazorCode = @"
<BitRichTextEditor @ref=""getEditorRef"" />

<BitButton OnClick=""GetText"">GetText</BitButton>
<BitButton OnClick=""GetHtml"">GetHtml</BitButton>
<BitButton OnClick=""GetContent"">GetContent</BitButton>

<div>result:</div>
<pre>@result</pre>";
    private readonly string example7CsharpCode = @"
private BitRichTextEditor getEditorRef = default!;
private string? result;
private async Task GetText()
{
    result = await getEditorRef.GetText();
}
private async Task GetHtml()
{
    result = await getEditorRef.GetHtml();
}
private async Task GetContent()
{
    result = await getEditorRef.GetContent();
}";

    private readonly string example8RazorCode = @"
<textarea @bind-value=""setValue"" @bind-value:event=""oninput"" style=""width:100%;height:100px"" />

<BitButton OnClick=""SetText"">SetText</BitButton>
<BitButton OnClick=""SetHtml"">SetHtml</BitButton>
<BitButton OnClick=""SetContent"">SetContent</BitButton>

<BitRichTextEditor @ref=""setEditorRef"" />";
    private readonly string example8CsharpCode = @"
private BitRichTextEditor setEditorRef = default!;
private string? setValue;
private async Task SetText()
{
    await setEditorRef.SetText(setValue);
}
private async Task SetHtml()
{
    await setEditorRef.SetHtml(setValue);
}
private async Task SetContent()
{
    await setEditorRef.SetContent(setValue);
}";

    private readonly string example9RazorCode = @"
<BitRichTextEditor>
    <ToolbarTemplate>
        <span class=""ql-formats"">
            <select class=""ql-font""></select>
            <select class=""ql-size""></select>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-bold""></button>
            <button class=""ql-italic""></button>
            <button class=""ql-underline""></button>
            <button class=""ql-strike""></button>
        </span>
        <span class=""ql-formats"">
            <select class=""ql-color""></select>
            <select class=""ql-background""></select>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-blockquote""></button>
            <button class=""ql-code-block""></button>
            <button class=""ql-link""></button>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-header"" value=""1""></button>
            <button class=""ql-header"" value=""2""></button>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-list"" value=""ordered""></button>
            <button class=""ql-list"" value=""bullet""></button>
            <button class=""ql-indent"" value=""-1""></button>
            <button class=""ql-indent"" value=""+1""></button>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-direction"" value=""rtl""></button>
            <select class=""ql-align""></select>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-script"" value=""sub""></button>
            <button class=""ql-script"" value=""super""></button>
        </span>
        <span class=""ql-formats"">
            <button class=""ql-clean""></button>
        </span>
    </ToolbarTemplate>
    <EditorTemplate>
        <div><b>this is bold</b></div>
        <div><em>this is italic</em></div>
    </EditorTemplate>
</BitRichTextEditor>";
}
