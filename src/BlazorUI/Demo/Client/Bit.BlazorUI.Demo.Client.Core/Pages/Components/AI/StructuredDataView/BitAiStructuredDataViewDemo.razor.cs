using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.StructuredDataView;

public partial class BitAiStructuredDataViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiStructuredDataViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the structured data view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage?",
            DefaultValue = "null",
            Description = "The message the part belongs to, handed to a renderer registered for the part's schema (in its BitAiStructuredDataContext) so it can read the rest of the answer. A new, empty assistant message stands in while it is null. The default rendering does not read it.",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiStructuredDataPart",
            DefaultValue = "null",
            Description = "The structured part to render (required). Its SchemaName picks a renderer registered with RegisterStructured; without one, its Value is drawn by its shape - an array of objects as a BitAiDataTable, an object as a list of labelled values, anything else as a BitAiJsonView. While nothing parseable has arrived, a streaming part shows a BitAiThinkingIndicator and a finished one nothing.",
            LinkType = LinkType.Link,
            Href = "#structured-data-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiStructuredDataViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the structured data view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiStructuredDataViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiStructuredDataView."
                },
                new()
                {
                    Name = "List",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the list of labelled values an object is shown as by the BitAiStructuredDataView."
                },
                new()
                {
                    Name = "Entry",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each entry - a label and its value - of the list of the BitAiStructuredDataView."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of each entry of the list of the BitAiStructuredDataView."
                },
                new()
                {
                    Name = "Value",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the value of each entry of the list of the BitAiStructuredDataView."
                },
                new()
                {
                    Name = "Empty",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dash a missing value is shown as in the list of the BitAiStructuredDataView."
                },
            ]
        },
        new()
        {
            Id = "structured-data-part",
            Title = "BitAiStructuredDataPart",
            Description = "A part of a message holding the structured output of a model: raw JSON that streams in and a lenient parse of it. A session creates and fills one from the BitAiStructuredDataDeltaEvent and BitAiStructuredDataEvent of its transport; an app creates one to show a value it has itself. Safe to read on the renderer's thread while it is appended to on the transport's.",
            Parameters =
            [
                new()
                {
                    Name = "new(schemaName?, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates an empty part that is streaming: Append the JSON as it arrives, then Complete it."
                },
                new()
                {
                    Name = "new(value, schemaName?, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a finished part holding a value."
                },
                new()
                {
                    Name = "SchemaName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A name identifying the shape of the value, which picks the renderer registered for it with BitAiRendererRegistry.RegisterStructured."
                },
                new()
                {
                    Name = "Value",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "The parsed value once complete; until then a lenient parse of the JSON received so far (BitAiPartialJson.TryParse, described on the AiJsonView page, /components/ai/jsonview#partial-json), null while nothing parseable has arrived."
                },
                new()
                {
                    Name = "RawJson",
                    Type = "string",
                    DefaultValue = "\"\"",
                    Description = "The raw JSON text received so far."
                },
                new()
                {
                    Name = "IsStreaming",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether more JSON may still arrive: true for a part created empty until Complete is called, false for one created with a value."
                },
                new()
                {
                    Name = "Append(delta)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Appends raw JSON text; Value is re-parsed the next time it is read."
                },
                new()
                {
                    Name = "Complete(value?)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Marks the part complete, with the value given or the accumulated text parsed strictly (falling back to the lenient parse when it is not valid JSON)."
                },
                new()
                {
                    Name = "As<T>(typeInfo)",
                    Type = "T?",
                    DefaultValue = "",
                    Description = "Deserializes the current value - partial or not - into a typed model with the metadata of the app's own JsonSerializerContext, safe to trim. Give the model nullable members: the fields not written yet are absent. Null when nothing parseable has arrived or the value does not fit the type."
                },
                new()
                {
                    Name = "As<T>(options?)",
                    Type = "T?",
                    DefaultValue = "",
                    Description = "The same with the reflection-based serializer (the web defaults when no options are given), marked as unsafe to trim and for native AOT."
                },
            ]
        },
        new()
        {
            Id = "renderer-registry",
            Title = "BitAiRendererRegistry",
            Description = "Where an app registers its own renderers, which the components resolve before their built-in views. A singleton: register at startup (AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(renderers => ...))) and treat the entries as fixed afterwards - every entry is app-wide. A region with renderers of its own cascades a registry of its own (new BitAiRendererRegistry(), in a CascadingValue), which every AI component inside it uses in place of the registered one. These are its structured-data members; the whole registry is on the AiMessagePartView page (/components/ai/messagepartview#renderer-registry).",
            Parameters =
            [
                new()
                {
                    Name = "RegisterStructured(schemaName, renderer)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Registers a BitAiStructuredDataRenderer for a schema name (case does not matter): every BitAiStructuredDataView of a part with that SchemaName renders it in place of the table, list or tree it would draw, inside a BitAiRendererBoundary; a later registration replaces an earlier one. A part with no SchemaName is never drawn by one.",
                    LinkType = LinkType.Link,
                    Href = "#structured-data-renderer",
                },
                new()
                {
                    Name = "FindStructured(schemaName)",
                    Type = "BitAiStructuredDataRenderer?",
                    DefaultValue = "",
                    Description = "The renderer registered for a schema name, or null (also for a null name) when the view reads the shape of the value itself.",
                },
            ]
        },
        new()
        {
            Id = "structured-data-renderer",
            Title = "BitAiStructuredDataRenderer",
            Description = "The delegate a custom structured-data renderer is: public delegate RenderFragment BitAiStructuredDataRenderer(BitAiStructuredDataContext context). It is called each time the view's parameters are set - on every render of a streaming answer - so it reads what it draws off the part as it renders; a part still streaming holds a partial Value (or none yet).",
            Parameters =
            [
                new()
                {
                    Name = "context",
                    Type = "BitAiStructuredDataContext",
                    DefaultValue = "",
                    Description = "The part and the message it belongs to.",
                    LinkType = LinkType.Link,
                    Href = "#structured-data-context",
                },
                new()
                {
                    Name = "(return value)",
                    Type = "RenderFragment",
                    DefaultValue = "",
                    Description = "The markup drawn in place of the built-in table, list or tree.",
                },
            ]
        },
        new()
        {
            Id = "structured-data-context",
            Title = "BitAiStructuredDataContext",
            Description = "What a renderer registered with BitAiRendererRegistry.RegisterStructured(schemaName, context => ...) is handed (a sealed record), through ConfigureRenderers of AddBitBlazorUIAIServices.",
            Parameters =
            [
                new()
                {
                    Name = "Part",
                    Type = "BitAiStructuredDataPart",
                    DefaultValue = "",
                    Description = "The part being rendered.",
                    LinkType = LinkType.Link,
                    Href = "#structured-data-part",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage",
                    DefaultValue = "",
                    Description = "The message the part belongs to: the Message parameter of the view, or a new assistant message when it is not set."
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiStructuredDataView-gap", DefaultValue = "spacing(1)", Description = "Room between two entries of the list of labelled values." },
        new() { Name = "--bit-AiStructuredDataView-padding", DefaultValue = "spacing(2)", Description = "Inset of the list." },
        new() { Name = "--bit-AiStructuredDataView-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the list." },
        new() { Name = "--bit-AiStructuredDataView-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the frame around the list." },
        new() { Name = "--bit-AiStructuredDataView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner of the frame." },
        new() { Name = "--bit-AiStructuredDataView-label-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the labels." },
        new() { Name = "--bit-AiStructuredDataView-label-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the labels." },
        new() { Name = "--bit-AiStructuredDataView-label-font-weight", DefaultValue = "var(--bit-tpg-fw-medium)", Description = "Weight of the labels." },
        new() { Name = "--bit-AiStructuredDataView-value-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the values." },
        new() { Name = "--bit-AiStructuredDataView-empty-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Color of the dash a missing value is shown as." },
    ];



    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private readonly BitAiStructuredDataPart flightPart = new(Parse("""
        { "flightNumber": "BA 117", "from": "LHR", "to": "JFK", "departure": "2026-11-02 09:40", "seats": 2, "refundable": false, "meal": null }
        """));

    private readonly BitAiStructuredDataPart ordersPart = new(Parse("""
        [
          { "order": "ord_4821", "status": "shipped", "total": 129.5 },
          { "order": "ord_4822", "status": "pending", "total": 42 }
        ]
        """));

    private readonly BitAiStructuredDataPart tagsPart = new(Parse("""["blazor", "webassembly", "streaming"]"""));

    private readonly BitAiStructuredDataPart customerPart = new(Parse("""
        { "name": "Ada Lovelace", "address": { "city": "London", "zip": "N1 9GU" }, "orders": ["ord_4821", "ord_4822"] }
        """));

    // Scripted transports (Services/AI): one streams four rows of JSON, the other the JSON Schema of a booking form.
    private readonly BitAiChatSession optionsSession = new(AiDemoContentTransports.Structured());
    private readonly BitAiChatSession bookingSession = new(AiDemoInteractiveTransports.ProposesForm());
    private BitAiRenderScheduler? renderScheduler;

    protected override void OnInitialized()
    {
        // A session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
        renderScheduler = new(() => InvokeAsync(StateHasChanged));
        optionsSession.StateChanged += renderScheduler.Request;
        bookingSession.StateChanged += renderScheduler.Request;
    }

    private BitAiMessage? OptionsAnswer => optionsSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

    private Task AskForOptionsAsync() => optionsSession.SubmitAsync("Compare the options");

    private BitAiMessage? BookingAnswer => bookingSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

    private Task ProposeBookingAsync() => bookingSession.SubmitAsync("Book a table for Friday");

    public async ValueTask DisposeAsync()
    {
        if (renderScheduler is not null) await renderScheduler.DisposeAsync();

        await optionsSession.DisposeAsync();
        await bookingSession.DisposeAsync();
    }

    private readonly BitAiStructuredDataViewParams[] structuredDataViewParams =
    [
        new() { Styles = new() { List = "border-radius: 1rem", Label = "color: var(--bit-clr-pri)" } },
    ];

    private readonly BitAiStructuredDataPart persianFlightPart = new(Parse("""
        { "شماره پرواز": "IR 721", "مبدا": "تهران", "مقصد": "لندن", "صندلی": 2, "قابل استرداد": true, "غذا": null }
        """));
}
