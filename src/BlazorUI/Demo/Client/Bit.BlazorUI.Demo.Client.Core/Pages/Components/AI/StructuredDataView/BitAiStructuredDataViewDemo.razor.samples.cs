namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.StructuredDataView;

public partial class BitAiStructuredDataViewDemo
{
    private const string FlightSampleCode = """"
private readonly BitAiStructuredDataPart flightPart = new(JsonDocument.Parse("""
    { "flightNumber": "BA 117", "from": "LHR", "to": "JFK", "departure": "2026-11-02 09:40", "seats": 2, "refundable": false, "meal": null }
    """).RootElement);
"""";

    private readonly string example1RazorCode = """
<BitAiStructuredDataView Part="flightPart" />
""";
    private readonly string example1CsharpCode = FlightSampleCode;

    private readonly string example2RazorCode = """
<BitAiStructuredDataView Part="ordersPart" />

<BitAiStructuredDataView Part="tagsPart" />

<BitAiStructuredDataView Part="customerPart" />
""";
    private readonly string example2CsharpCode = """"
private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

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
"""";

    private readonly string example3RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskForOptionsAsync" Disabled="optionsSession.IsGenerating">Compare the options</BitButton>

@if (OptionsAnswer is { } answer)
{
    @foreach (var part in answer.PartsOf<BitAiStructuredDataPart>())
    {
        <BitAiStructuredDataView Part="part" Message="answer" />
    }
}
""";
    private readonly string example3CsharpCode = """
// A scripted transport that streams four rows of JSON a few characters at a time (see the next tab).
private readonly BitAiChatSession optionsSession = new(AiDemoContentTransports.Structured());
private BitAiRenderScheduler? renderScheduler;

protected override void OnInitialized()
{
    // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    optionsSession.StateChanged += renderScheduler.Request;
}

private BitAiMessage? OptionsAnswer => optionsSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

private Task AskForOptionsAsync() => optionsSession.SubmitAsync("Compare the options");

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await optionsSession.DisposeAsync();
}
""";
    private const string example3TransportCode = """"
public static IBitAiTransport Structured() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        const string intro = "Four candidates, ranked by how much work each one would be.\n\n";
        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        const string json = """
            [
              {"option":"Server-side rendering","effort":"Low","risk":"Low","notes":"Fastest to ship"},
              {"option":"WebAssembly","effort":"Medium","risk":"Low","notes":"Best offline story"},
              {"option":"Hybrid per page","effort":"Medium","risk":"Medium","notes":"Two mental models"},
              {"option":"Rewrite the front end","effort":"High","risk":"High","notes":"Not this quarter"}
            ]
            """;

        // Fourteen characters at a time: the JSON is incomplete until the last slice.
        foreach (var slice in AiScript.Slice(json, 14))
        {
            await AiScript.Pause(70, token);
            await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "options" });
        }

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example3CodeFiles =
    [
        new("AiDemoContentTransports.cs", example3TransportCode),
    ];

    private readonly string example4RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="ProposeBookingAsync" Disabled="bookingSession.IsGenerating">Book a table for Friday</BitButton>

@if (BookingAnswer is { } booking)
{
    @foreach (var part in booking.PartsOf<BitAiStructuredDataPart>())
    {
        <BitAiStructuredDataView Part="part" Message="booking" />
    }
}
""";
    private readonly string example4CsharpCode = """
// A scripted transport that streams the JSON Schema of a form, under the schema name booking_form (see the last tab).
private readonly BitAiChatSession bookingSession = new(AiDemoInteractiveTransports.ProposesForm());
private BitAiRenderScheduler? renderScheduler;

protected override void OnInitialized()
{
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    bookingSession.StateChanged += renderScheduler.Request;
}

private BitAiMessage? BookingAnswer => bookingSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

private Task ProposeBookingAsync() => bookingSession.SubmitAsync("Book a table for Friday");

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await bookingSession.DisposeAsync();
}
""";
    private const string example4ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport<MyTransport>();

    // A schema the model fills in becomes a form the user checks, instead of raw JSON.
    ai.ConfigureRenderers(renderers => renderers.RegisterStructured("booking_form", context => builder =>
    {
        builder.OpenComponent<BookingForm>(0);
        builder.AddComponentParameter(1, nameof(BookingForm.Part), context.Part);
        builder.CloseComponent();
    }));
});
""";
    private const string example4FormCode = """
@* Not submitted before the whole schema has arrived, and not edited after it was. *@
<BitAiForm Definition="definition"
           Busy="Part.IsStreaming"
           Disabled="submitted"
           Submitted="submitted"
           OnSubmit="OnSubmitAsync" />

@code {
    private BitAiFormDefinition definition = new();
    private bool submitted;

    [Parameter, EditorRequired] public BitAiStructuredDataPart Part { get; set; } = default!;

    [CascadingParameter(Name = "BookingSubmitted")]
    public EventCallback<IReadOnlyDictionary<string, string?>> Submitted { get; set; }

    // Rebuilt as the schema streams in; the form keeps whatever the user already typed.
    protected override void OnParametersSet() =>
        definition = Part.Value is { } schema ? BitAiFormDefinition.FromJsonSchema(schema) : new BitAiFormDefinition();

    private async Task OnSubmitAsync(IReadOnlyDictionary<string, string?> values)
    {
        submitted = true;
        await Submitted.InvokeAsync(values);
    }
}
""";
    private const string example4TransportCode = """"
public static IBitAiTransport ProposesForm() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        const string intro = "I can book that. Check the details first, since I inferred two of them.\n\n";
        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        // The two details the model inferred go in as the defaults the form starts from: two guests, and the coming Friday.
        var today = DateTime.Today;
        var friday = today.AddDays(((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7);
        var date = friday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var schema = $$"""
            {
              "title": "Book a table",
              "type": "object",
              "required": ["restaurant", "guests", "date"],
              "properties": {
                "restaurant": { "type": "string", "title": "Restaurant" },
                "guests": { "type": "integer", "title": "Guests", "minimum": 1, "maximum": 12, "default": 2 },
                "date": { "type": "string", "format": "date", "title": "Date", "default": "{{date}}" },
                "seating": { "type": "string", "enum": ["Inside", "Terrace", "No preference"] },
                "notes": { "type": "string", "format": "textarea", "title": "Notes for the restaurant" }
              }
            }
            """;

        // Named booking_form, which is what picks the registered renderer.
        foreach (var slice in AiScript.Slice(schema, 30))
        {
            await AiScript.Pause(50, token);
            await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "booking_form" });
        }

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example4CodeFiles =
    [
        new("Program.cs", example4ProgramCode),
        new("BookingForm.razor", example4FormCode),
        new("AiDemoInteractiveTransports.cs", example4TransportCode),
    ];

    private readonly string example5RazorCode = """
<style>
    .custom-list {
        border-width: 0;
        border-inline-start-width: 0.25rem;
        border-radius: 0;
        border-color: #8a2be2;
    }

    .custom-entry {
        flex-direction: row;
        align-items: baseline;
        gap: 0.5rem;
    }

    .custom-label::after {
        content: ":";
    }
</style>

<BitAiStructuredDataView Part="flightPart"
                         Style="max-width: 24rem"
                         Styles="@(new() { List = "background-color: #1e1e2e; border-color: #1e1e2e", Label = "color: #89b4fa", Value = "color: #cdd6f4" })" />

<BitAiStructuredDataView Part="flightPart" Classes="@(new() { List = "custom-list", Entry = "custom-entry", Label = "custom-label" })" />

<div style="--bit-AiStructuredDataView-background: transparent; --bit-AiStructuredDataView-border-color: #d2691e; --bit-AiStructuredDataView-label-color: #d2691e; --bit-AiStructuredDataView-gap: 0.25rem">
    <BitAiStructuredDataView Part="flightPart" />
</div>
""";
    private readonly string example5CsharpCode = FlightSampleCode;

    private readonly string example6RazorCode = """
<BitAiStructuredDataView Dir="BitDir.Rtl" Part="persianFlightPart" />

<div dir="rtl">
    <BitAiStructuredDataView Part="persianFlightPart" />
</div>
""";
    private readonly string example6CsharpCode = """"
private readonly BitAiStructuredDataPart persianFlightPart = new(JsonDocument.Parse("""
    { "شماره پرواز": "IR 721", "مبدا": "تهران", "مقصد": "لندن", "صندلی": 2, "قابل استرداد": true, "غذا": null }
    """).RootElement);
"""";
}
