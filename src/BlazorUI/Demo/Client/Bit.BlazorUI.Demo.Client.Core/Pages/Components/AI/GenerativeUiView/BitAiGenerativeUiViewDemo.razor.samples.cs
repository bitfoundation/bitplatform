namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.GenerativeUiView;

public partial class BitAiGenerativeUiViewDemo
{
    private const string CardSampleCode = """
// A finished part, as a session leaves it once the model has written the properties.
private static BitAiGenerativeUiPart Card(string componentName, string props)
{
    var part = new BitAiGenerativeUiPart(componentName);
    part.SetProps(JsonDocument.Parse(props).RootElement);
    part.Complete();
    return part;
}
""";

    private readonly string example1RazorCode = """
<BitAiGenerativeUiView Part="osloPart" />
""";
    private readonly string example1CsharpCode = CardSampleCode + """"

private readonly BitAiGenerativeUiPart osloPart = Card("forecast_card", """{ "city": "Oslo", "degrees": -3, "condition": "Snow", "humidity": 81 }""");
"""";

    private readonly string example2RazorCode = """
<BitButton OnClick="StreamForecastAsync" Disabled="isStreamingForecast">Stream a forecast</BitButton>

@if (streamedPart is not null)
{
    <BitAiGenerativeUiView Part="streamedPart" />
}
""";
    private readonly string example2CsharpCode = """"
private const string LisbonForecast = """{ "city": "Lisbon", "degrees": 17, "condition": "Clear", "humidity": 54 }""";

private bool isStreamingForecast;
private BitAiGenerativeUiPart? streamedPart;

private async Task StreamForecastAsync()
{
    isStreamingForecast = true;
    streamedPart = new BitAiGenerativeUiPart("forecast_card");

    // Five characters at a time, the way a model writes it.
    for (var i = 0; i < LisbonForecast.Length; i += 5)
    {
        streamedPart.AppendProps(LisbonForecast.Substring(i, Math.Min(5, LisbonForecast.Length - i)));
        StateHasChanged();

        await Task.Delay(80);
    }

    streamedPart.Complete();
    isStreamingForecast = false;
}
"""";

    private readonly string example3RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskForForecastsAsync" Disabled="forecastSession.IsGenerating">The weather in three cities</BitButton>

@if (ForecastAnswer is { } answer)
{
    @foreach (var part in answer.PartsOf<BitAiGenerativeUiPart>())
    {
        <BitAiGenerativeUiView Part="part" />
    }
}
""";
    private readonly string example3CsharpCode = """
// A scripted transport that asks for three forecast_card components between two lines of text (see the next tab).
private readonly BitAiChatSession forecastSession = new(AiDemoInteractiveTransports.GenerativeUi());
private BitAiRenderScheduler? renderScheduler;

protected override void OnInitialized()
{
    // The session says so on every streamed event; the scheduler turns any number of those into one render per 50 ms.
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    forecastSession.StateChanged += renderScheduler.Request;
}

private BitAiMessage? ForecastAnswer => forecastSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

private Task AskForForecastsAsync() => forecastSession.SubmitAsync("The weather in Oslo, Lisbon and Auckland");

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await forecastSession.DisposeAsync();
}
""";
    private const string example3TransportCode = """"
public static IBitAiTransport GenerativeUi() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        const string intro = "Here is the weather right now in the three cities you asked about.\n\n";
        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        string[] cities =
        [
            """{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""",
            """{"city":"Lisbon","degrees":17,"condition":"Clear","humidity":54}""",
            """{"city":"Auckland","degrees":22,"condition":"Showers","humidity":72}""",
        ];

        // Each event names a registered component and carries its properties.
        foreach (var json in cities)
        {
            await AiScript.Pause(350, token);
            await yielder(new BitAiGenerativeUiEvent("forecast_card", AiScript.Json(json)));
        }

        const string outro = "\n\nOslo is the outlier. If the trip is flexible, move it a month.";
        await foreach (var evt in AiScript.Write(outro, 4, 12, token)) await yielder(evt);
        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example3CodeFiles =
    [
        new("AiDemoInteractiveTransports.cs", example3TransportCode),
    ];

    private readonly string example4RazorCode = """
@inject BitAiGenerativeUiRegistry GenerativeRegistry

<BitAiJsonView Value="registrySchema" DefaultExpandDepth="8" />
""";
    private readonly string example4CsharpCode = """
private JsonElement registrySchema;

// The JSON Schema of every registered component, to hand the model.
protected override void OnInitialized() => registrySchema = GenerativeRegistry.DescribeAll();
""";
    private const string example4ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport<MyTransport>();

    // The components model output is allowed to instantiate, and the properties it may set.
    ai.ConfigureGenerativeUi(RegisterGenerativeComponents);
});

// Registering reads the parameters by reflection, which the registry marks as unsafe to trim. It is safe here: Register
// keeps the component type whole through its annotation, and the parameters it binds are strings and integers, which
// have no members of their own for the trimmer to take away. An app binding records of its own keeps them instead.
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ForecastCard is kept whole by the annotation of Register, and its bindable parameters are strings and integers.")]
[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "ForecastCard's bindable parameters are strings and integers, which need no code generated at run time.")]
static void RegisterGenerativeComponents(BitAiGenerativeUiRegistry registry)
{
    // The name the model uses, what the component is for, and the properties it must supply.
    registry.Register<ForecastCard>("forecast_card", "Shows the current weather for one city.", "City");
}
""";
    private const string example4CardCode = """
@* A component the model is allowed to render, in the AI generative UI scenario.

   Nothing about it knows it is generative: it is an ordinary Blazor component with ordinary parameters. The registry
   (BitAiGenerativeUiRegistry, filled in AddClientSharedServices) decides that model output may instantiate it, and the
   binder decides which of its parameters model output may set. *@
<div class="forecast-card">
    <div class="forecast-card-city">@City</div>
    <div class="forecast-card-temp">@Degrees&deg;</div>
    <div class="forecast-card-meta">
        <span>@Condition</span>
        @if (Humidity > 0)
        {
            <span>@Humidity% humidity</span>
        }
    </div>
</div>

@code {
    /// <summary>The city being forecast.</summary>
    [Parameter, System.ComponentModel.Description("The city name.")]
    public string City { get; set; } = string.Empty;

    /// <summary>The temperature in Celsius.</summary>
    [Parameter, System.ComponentModel.Description("Temperature in degrees Celsius.")]
    public int Degrees { get; set; }

    /// <summary>A one-word summary such as Clear or Snow.</summary>
    [Parameter, System.ComponentModel.Description("One word summary, for example Clear, Snow, Showers.")]
    public string Condition { get; set; } = string.Empty;

    /// <summary>Relative humidity as a percentage.</summary>
    [Parameter, System.ComponentModel.Description("Relative humidity as a percentage.")]
    public int Humidity { get; set; }
}
""";
    private readonly DemoCodeFile[] example4CodeFiles =
    [
        new("Program.cs", example4ProgramCode),
        new("ForecastCard.razor", example4CardCode),
    ];

    private readonly string example5RazorCode = """
<BitAiGenerativeUiView Part="unregisteredPart" />
""";
    private readonly string example5CsharpCode = CardSampleCode + """"

private readonly BitAiGenerativeUiPart unregisteredPart = Card("stock_ticker", """{ "symbol": "BIT", "price": 42.5 }""");
"""";

    private const string FaultySampleCode = CardSampleCode + """"

// degrees is not a number, City (required) is missing, and wind is not a parameter of ForecastCard.
private readonly BitAiGenerativeUiPart faultyPart = Card("forecast_card", """{ "degrees": "very cold", "condition": "Windy", "wind": "40 km/h" }""");
"""";

    private readonly string example6RazorCode = """
<BitAiGenerativeUiView Part="faultyPart" ShowBindingErrors />
""";
    private readonly string example6CsharpCode = FaultySampleCode;

    private readonly string example7RazorCode = """
<BitToggle @bind-Value="showDiagnostics" Text="Show binding errors" />

<BitParams Parameters="generativeParams">
    <BitAiGenerativeUiView Part="faultyPart" />
    <BitAiGenerativeUiView Part="faultyPart" ShowBindingErrors="false" />
</BitParams>
""";
    private readonly string example7CsharpCode = FaultySampleCode + """

private bool showDiagnostics = true;
private BitAiGenerativeUiViewParams[] generativeParams => [new() { ShowBindingErrors = showDiagnostics }];
""";

    private readonly string example8RazorCode = """
<style>
    .custom-diagnostics {
        padding: 0.5rem;
        border-radius: 0.5rem;
        border: 1px dashed #d2691e;
    }
</style>

<BitAiGenerativeUiView Part="osloPart"
                       Style="padding: 0.5rem; border-radius: 0.5rem; background-color: #8a2be21a"
                       Styles="@(new() { Content = "display: inline-block; transform: rotate(-2deg)" })" />

<BitAiGenerativeUiView Part="faultyPart" ShowBindingErrors Classes="@(new() { Diagnostics = "custom-diagnostics" })" />

<div style="--bit-AiGenerativeUiView-diagnostics-gap: 0.25rem; --bit-AiGenerativeUiView-diagnostics-margin: 0">
    <BitAiGenerativeUiView Part="faultyPart" ShowBindingErrors />
</div>
""";
    private readonly string example8CsharpCode = CardSampleCode + """"

private readonly BitAiGenerativeUiPart osloPart = Card("forecast_card", """{ "city": "Oslo", "degrees": -3, "condition": "Snow", "humidity": 81 }""");

private readonly BitAiGenerativeUiPart faultyPart = Card("forecast_card", """{ "degrees": "very cold", "condition": "Windy", "wind": "40 km/h" }""");
"""";

    private readonly string example9RazorCode = """
<BitAiGenerativeUiView Dir="BitDir.Rtl" Part="tehranPart" />

<div dir="rtl">
    <BitAiGenerativeUiView Part="unregisteredPart" />
</div>
""";
    private readonly string example9CsharpCode = CardSampleCode + """"

private readonly BitAiGenerativeUiPart tehranPart = Card("forecast_card", """{ "city": "تهران", "degrees": 24, "condition": "آفتابی", "humidity": 30 }""");

private readonly BitAiGenerativeUiPart unregisteredPart = Card("stock_ticker", """{ "symbol": "BIT", "price": 42.5 }""");
"""";
}
