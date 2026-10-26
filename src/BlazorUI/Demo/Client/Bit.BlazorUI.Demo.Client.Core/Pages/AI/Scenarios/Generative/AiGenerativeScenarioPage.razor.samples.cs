namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Generative;

public partial class AiGenerativeScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask about three cities"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.GenerativeUi());

private readonly string[] suggestions = ["What is the weather like in Oslo, Lisbon and Auckland?"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The components model output is allowed to instantiate, and the properties it must supply.
    ai.ConfigureGenerativeUi(RegisterGenerativeComponents);
});

// Register reads the component's parameters by reflection, which it marks as unsafe to trim.
// Safe here: the annotation of Register keeps ForecastCard whole, and its parameters are plain data.
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ForecastCard is kept whole by the annotation of Register, and its bindable parameters are strings and integers.")]
[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "ForecastCard's bindable parameters are strings and integers, which need no code generated at run time.")]
static void RegisterGenerativeComponents(BitAiGenerativeUiRegistry registry)
{
    registry.Register<ForecastCard>("forecast_card", "Shows the current weather for one city.", "City");
}
""";

    private const string example1CardCode = """
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
    // The descriptions go into the schema the model is shown.
    [Parameter, System.ComponentModel.Description("The city name.")]
    public string City { get; set; } = string.Empty;

    [Parameter, System.ComponentModel.Description("Temperature in degrees Celsius.")]
    public int Degrees { get; set; }

    [Parameter, System.ComponentModel.Description("One word summary, for example Clear, Snow, Showers.")]
    public string Condition { get; set; } = string.Empty;

    [Parameter, System.ComponentModel.Description("Relative humidity as a percentage.")]
    public int Humidity { get; set; }
}
""";

    private const string example1TransportCode = """"
public static IBitAiTransport GenerativeUi() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());
    await foreach (var evt in AiScript.Write("Here is the weather right now in the three cities you asked about.\n\n", 4, 12, token))
        await yielder(evt);

    // A component, named as it was registered, and the properties to bind onto it.
    await yielder(new BitAiGenerativeUiEvent("forecast_card",
        AiScript.Json("""{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""")));
    await yielder(new BitAiGenerativeUiEvent("forecast_card",
        AiScript.Json("""{"city":"Lisbon","degrees":17,"condition":"Clear","humidity":54}""")));
    await yielder(new BitAiGenerativeUiEvent("forecast_card",
        AiScript.Json("""{"city":"Auckland","degrees":22,"condition":"Showers","humidity":72}""")));

    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("ForecastCard.razor", example1CardCode),
        new("AiDemoInteractiveTransports.cs", example1TransportCode),
    ];
}
