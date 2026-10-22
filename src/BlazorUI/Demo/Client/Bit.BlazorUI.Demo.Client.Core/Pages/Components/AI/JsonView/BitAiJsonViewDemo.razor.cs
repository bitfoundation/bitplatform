using System.Text.Json;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.JsonView;

public partial class BitAiJsonViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiJsonViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the JSON view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DefaultExpandDepth",
            Type = "int",
            DefaultValue = "2",
            Description = "How many levels of the tree are expanded before the containers below them start collapsed: 0 starts the root collapsed. A container the user toggled keeps the state they gave it when this changes; the ones nobody touched follow the new depth.",
        },
        new()
        {
            Name = "LargeContainerSize",
            Type = "int",
            DefaultValue = "24",
            Description = "The number of entries past which an object or an array starts collapsed whatever its depth, so a long result costs one row until it is opened.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiJsonViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the JSON view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Value",
            Type = "JsonElement?",
            DefaultValue = "null",
            Description = "The JSON value to render: an object or an array as a collapsible container, anything else as a single row. Nothing is rendered inside the view while it is null. A partial payload repaired by BitAiPartialJson.TryParse renders as far as it has arrived.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiJsonViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiJsonView."
                },
                new()
                {
                    Name = "Row",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for every row of the BitAiJsonView: the head of a container, the row of its closing brace and the row of a scalar."
                },
                new()
                {
                    Name = "Toggle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle buttons that expand and collapse the containers of the BitAiJsonView."
                },
                new()
                {
                    Name = "ToggleIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the chevron icon of the toggle buttons of the BitAiJsonView."
                },
                new()
                {
                    Name = "Group",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the indented group of the entries of an open container of the BitAiJsonView."
                },
                new()
                {
                    Name = "Key",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the property keys of the BitAiJsonView."
                },
                new()
                {
                    Name = "String",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the string values of the BitAiJsonView."
                },
                new()
                {
                    Name = "Number",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number values of the BitAiJsonView."
                },
                new()
                {
                    Name = "Boolean",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the true and false values of the BitAiJsonView."
                },
                new()
                {
                    Name = "Null",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the null values of the BitAiJsonView."
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiJsonView-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the text of the tree, set in the monospaced font of the theme." },
        new() { Name = "--bit-AiJsonView-line-height", DefaultValue = "1.5", Description = "Height of a row." },
        new() { Name = "--bit-AiJsonView-indent", DefaultValue = "spacing(2)", Description = "Indent of the entries of an open container, from the inline-start edge." },
        new() { Name = "--bit-AiJsonView-key-color", DefaultValue = "var(--bit-clr-wrn-fg)", Description = "Color of the property keys." },
        new() { Name = "--bit-AiJsonView-string-color", DefaultValue = "var(--bit-clr-suc-fg)", Description = "Color of the string values." },
        new() { Name = "--bit-AiJsonView-number-color", DefaultValue = "var(--bit-clr-sec-fg)", Description = "Color of the number values." },
        new() { Name = "--bit-AiJsonView-boolean-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Color of true and false." },
        new() { Name = "--bit-AiJsonView-null-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Color of null." },
        new() { Name = "--bit-AiJsonView-toggle-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the toggles: the chevrons and the braces of the containers, and their entry counts while collapsed. A disabled tree draws them in the disabled color instead." },
        new() { Name = "--bit-AiJsonView-chevron-size", DefaultValue = "var(--bit-tpg-fs-2xs)", Description = "Size of the chevron of a toggle." },
    ];



    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private readonly JsonElement order = Parse("""
        {
          "id": "ord_4821",
          "status": "shipped",
          "total": 129.5,
          "paid": true,
          "coupon": null,
          "customer": { "name": "Ada Lovelace", "email": "ada@example.com", "address": { "city": "London", "zip": "N1 9GU" } },
          "items": [
            { "sku": "KB-01", "name": "Keyboard", "quantity": 1, "price": 89.5 },
            { "sku": "MS-02", "name": "Mouse", "quantity": 2, "price": 20 }
          ]
        }
        """);

    private double expandDepth = 1;

    private readonly JsonElement readings = Parse($"[{string.Join(", ", Enumerable.Range(1, 30).Select(i => 18 + i % 7))}]");

    private const string ToolResult = """
        {
          "query": "render modes",
          "took_ms": 42,
          "hits": [
            { "title": "Render modes", "score": 0.94, "url": "https://example.com/docs/render-modes" },
            { "title": "Circuit lifetime", "score": 0.81, "url": "https://example.com/docs/circuits" },
            { "title": "Prerendering", "score": 0.66, "url": "https://example.com/docs/prerendering" }
          ],
          "truncated": false
        }
        """;

    private bool isStreaming;
    private JsonElement? streamedValue;

    private async Task StreamResultAsync()
    {
        isStreaming = true;

        // Nine characters at a time, the way a model writes it, repaired into a valid document on every step.
        for (var length = 9; length < ToolResult.Length + 9; length += 9)
        {
            streamedValue = BitAiPartialJson.TryParse(ToolResult[..Math.Min(length, ToolResult.Length)]);
            StateHasChanged();

            await Task.Delay(60);
        }

        isStreaming = false;
    }

    private readonly JsonElement persianOrder = Parse("""
        {
          "شناسه": "ord_4821",
          "وضعیت": "ارسال شده",
          "جمع": 129.5,
          "پرداخت شده": true,
          "مشتری": { "نام": "آدا لاولیس", "شهر": "لندن" },
          "اقلام": [ { "نام": "صفحه کلید", "تعداد": 1 }, { "نام": "ماوس", "تعداد": 2 } ]
        }
        """);
}
