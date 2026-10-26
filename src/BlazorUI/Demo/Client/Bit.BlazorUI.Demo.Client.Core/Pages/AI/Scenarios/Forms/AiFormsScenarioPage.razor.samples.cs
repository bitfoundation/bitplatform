namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Forms;

public partial class AiFormsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <CascadingValue Name="BookingSubmitted" Value="onSubmitted" IsFixed="true">
        <BitAiChat Session="session"
                   EmptyTitle="Ask it to book something"
                   Suggestions="suggestions" />
    </CascadingValue>
</div>

@if (values is not null)
{
    <h4>Submitted values</h4>
    <ul>
        @foreach (var (key, value) in values)
        {
            <li><code>@key</code>: @value</li>
        }
    </ul>
}
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.ProposesForm());
private IReadOnlyDictionary<string, string?>? values;
private EventCallback<IReadOnlyDictionary<string, string?>> onSubmitted;

private readonly string[] suggestions = ["Book a table for Friday"];

// What BookingForm calls with the corrected values - the page decides what a booking means.
protected override void OnInitialized() =>
    onSubmitted = EventCallback.Factory.Create<IReadOnlyDictionary<string, string?>>(this, submitted => values = submitted);

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // A schema the model fills in becomes a form the user checks, instead of raw JSON.
    ai.ConfigureRenderers(renderers => renderers.RegisterStructured("booking_form", context => builder =>
    {
        builder.OpenComponent<BookingForm>(0);
        builder.AddComponentParameter(1, nameof(BookingForm.Part), context.Part);
        builder.CloseComponent();
    }));
});
""";

    private const string example1FormCode = """
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

    private const string example1TransportCode = """"
public static IBitAiTransport ProposesForm() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());
    await foreach (var evt in AiScript.Write("I can book that. Check the details first, since I inferred two of them.\n\n", 4, 12, token))
        await yielder(evt);

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

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("BookingForm.razor", example1FormCode),
        new("AiDemoInteractiveTransports.cs", example1TransportCode),
    ];
}
