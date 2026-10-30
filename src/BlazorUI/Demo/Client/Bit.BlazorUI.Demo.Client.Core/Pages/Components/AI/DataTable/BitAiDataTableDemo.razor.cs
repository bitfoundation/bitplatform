using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.DataTable;

public partial class BitAiDataTableDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiDataTableClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the data table.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Rows",
            Type = "JsonElement",
            DefaultValue = "default",
            Description = "The JSON array of objects to render (required): one row per item and one column per key, the columns being the union of the keys of every item in the order they first appear. A cell shows a string as its text, a number or a boolean as the JSON it was written as, a nested object or array as its JSON text, and a null or missing field as nothing. Anything but an array renders an empty table, and an item that is not an object an empty row. A partial payload repaired by BitAiPartialJson.TryParse renders as far as it has arrived.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiDataTableClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the data table.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiDataTableClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiDataTable, the box the table scrolls in sideways."
                },
                new()
                {
                    Name = "Table",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the table element of the BitAiDataTable."
                },
                new()
                {
                    Name = "HeaderRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header row of the BitAiDataTable."
                },
                new()
                {
                    Name = "HeaderCell",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each header cell of the BitAiDataTable."
                },
                new()
                {
                    Name = "Row",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each body row of the BitAiDataTable."
                },
                new()
                {
                    Name = "Cell",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each body cell of the BitAiDataTable."
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiDataTable-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the text of the table." },
        new() { Name = "--bit-AiDataTable-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the text of the cells." },
        new() { Name = "--bit-AiDataTable-background", DefaultValue = "transparent", Description = "Background of the table." },
        new() { Name = "--bit-AiDataTable-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the frame around the table." },
        new() { Name = "--bit-AiDataTable-divider-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Color of the rules between the rows." },
        new() { Name = "--bit-AiDataTable-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner of the frame." },
        new() { Name = "--bit-AiDataTable-cell-padding", DefaultValue = "spacing(1) spacing(1.5)", Description = "Inset of every cell, header cells included." },
        new() { Name = "--bit-AiDataTable-header-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the header row." },
        new() { Name = "--bit-AiDataTable-header-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the text of the header row." },
        new() { Name = "--bit-AiDataTable-header-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the text of the header row." },
    ];



    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private readonly JsonElement models = Parse("""
        [
          { "model": "Demo Large", "contextWindow": 200000, "streaming": true, "price": 15 },
          { "model": "Demo Fast", "contextWindow": 128000, "streaming": true, "price": 1.5 },
          { "model": "Local Small", "contextWindow": 32000, "streaming": false, "price": 0 }
        ]
        """);

    private readonly JsonElement mixed = Parse("""
        [
          { "id": 1, "name": "Ada", "active": true, "lastLogin": "2026-10-20" },
          { "id": 2, "name": "Grace", "active": false, "tags": ["compilers", "navy"] },
          { "id": 3, "name": "Linus", "lastLogin": null, "owner": { "team": "kernel" }, "time_zone": "UTC+2" }
        ]
        """);

    // A scripted transport (Services/AI) that streams four rows of JSON a few characters at a time.
    private readonly BitAiChatSession optionsSession = new(AiDemoContentTransports.Structured());
    private BitAiRenderScheduler? renderScheduler;

    protected override void OnInitialized()
    {
        // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
        renderScheduler = new(() => InvokeAsync(StateHasChanged));
        optionsSession.StateChanged += renderScheduler.Request;
    }

    // The structured part of the latest answer, whose Value is a lenient parse of the JSON received so far.
    private BitAiStructuredDataPart? OptionsPart =>
        optionsSession.Conversation.LastMessage?.PartsOf<BitAiStructuredDataPart>().FirstOrDefault();

    private Task AskForOptionsAsync() => optionsSession.SubmitAsync("Compare the options");

    public async ValueTask DisposeAsync()
    {
        if (renderScheduler is not null) await renderScheduler.DisposeAsync();

        await optionsSession.DisposeAsync();
    }

    private readonly JsonElement benchmarks = Parse("""
        [
          { "model": "Demo Large", "contextWindow": 200000, "maxOutput": 32000, "inputPrice": 15, "outputPrice": 75, "latencyMs": 820, "tokensPerSecond": 64, "vision": true, "tools": true, "jsonMode": true, "region": "eu-west", "released": "2026-03-01" },
          { "model": "Demo Fast", "contextWindow": 128000, "maxOutput": 16000, "inputPrice": 1.5, "outputPrice": 6, "latencyMs": 240, "tokensPerSecond": 180, "vision": true, "tools": true, "jsonMode": true, "region": "us-east", "released": "2026-06-15" },
          { "model": "Local Small", "contextWindow": 32000, "maxOutput": 4000, "inputPrice": 0, "outputPrice": 0, "latencyMs": 120, "tokensPerSecond": 95, "vision": false, "tools": false, "jsonMode": true, "region": "on-device", "released": "2025-11-30" }
        ]
        """);

    private readonly BitAiDataTableParams[] dataTableParams =
    [
        new() { Styles = new() { HeaderCell = "text-transform: uppercase", Row = "font-variant-numeric: tabular-nums" } },
    ];

    private readonly JsonElement persianModels = Parse("""
        [
          { "مدل": "دمو بزرگ", "پنجره زمینه": 200000, "قیمت": 15 },
          { "مدل": "دمو سریع", "پنجره زمینه": 128000, "قیمت": 1.5 }
        ]
        """);
}
