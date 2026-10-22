namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.RendererBoundary;

public partial class BitAiRendererBoundaryDemo
{
    private readonly string example1RazorCode = """
<BitAiRendererBoundary>
    <WeatherCard Json="@osloForecast" />
</BitAiRendererBoundary>

<BitAiRendererBoundary>
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>
""";
    private readonly string example1CsharpCode = """"
private readonly string osloForecast = """{"city":"Oslo","degrees":-3,"condition":"Snow"}""";

// The same forecast in a shape the card was not written for: its temperature is an object, not "degrees".
private readonly string unexpectedForecast = """{"city":"Lisbon","temperature":{"value":17,"unit":"C"},"condition":"Clear"}""";
"""";
    private const string example1CardCode = """
@using System.Text.Json

<div class="weather-card">
    <BitIcon IconName="@BitIconName.Sunny" />
    <div>
        <div class="weather-card-city">@city</div>
        <div>@degrees °C, @condition</div>
    </div>
</div>

@code {
    /// <summary>The forecast a model produced, as JSON.</summary>
    [Parameter, EditorRequired] public string Json { get; set; } = default!;

    private string? city;
    private int degrees;
    private string? condition;

    // Written for the complete shape only: a partial payload, or one without a field, throws here - which is the
    // failure a BitAiRendererBoundary around the card contains.
    protected override void OnParametersSet()
    {
        using var document = JsonDocument.Parse(Json);
        var root = document.RootElement;

        city = root.GetProperty("city").GetString();
        degrees = root.GetProperty("degrees").GetInt32();
        condition = root.GetProperty("condition").GetString();
    }
}
""";
    private const string example1CardStyleCode = """
.weather-card {
    gap: 0.75rem;
    display: flex;
    max-width: 20rem;
    align-items: center;
    box-sizing: border-box;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    color: var(--bit-clr-fg-pri);
    background-color: var(--bit-clr-bg-sec);
    border: 1px solid var(--bit-clr-brd-sec);
}

.weather-card-city {
    font-weight: 600;
}
""";
    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("WeatherCard.razor", example1CardCode),
        new("WeatherCard.razor.scss", example1CardStyleCode),
    ];

    private readonly string example2RazorCode = """
<BitAiRendererBoundary Label="The weather card could not show this forecast">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>
""";
    private readonly string example2CsharpCode = """"
private readonly string unexpectedForecast = """{"city":"Lisbon","temperature":{"value":17,"unit":"C"},"condition":"Clear"}""";
"""";

    private readonly string example3RazorCode = """
<BitButton OnClick="StreamForecastAsync" Disabled="isStreaming">Stream the forecast</BitButton>

<code>@(streamed.Length > 0 ? streamed : "nothing received yet")</code>

@if (streamed.Length > 0)
{
    <BitAiRendererBoundary Label="Waiting for the complete forecast">
        <WeatherCard Json="@streamed" />
    </BitAiRendererBoundary>
}
""";
    private readonly string example3CsharpCode = """"
private const string aucklandForecast = """{"city":"Auckland","degrees":22,"condition":"Showers"}""";
private string streamed = "";
private bool isStreaming;

private async Task StreamForecastAsync()
{
    isStreaming = true;
    streamed = "";

    // A model writes the JSON a few characters at a time; every render on the way tries the card again.
    foreach (var chunk in aucklandForecast.Chunk(12))
    {
        streamed += new string(chunk);
        StateHasChanged();
        await Task.Delay(400);
    }

    isStreaming = false;
}
"""";

    private readonly string example4RazorCode = """
<style>
    .custom-boundary {
        padding: 0.5rem;
        border-inline-start: 4px solid #b91c1c;
    }

    .custom-boundary-root {
        text-align: center;
    }

    .custom-boundary-icon {
        color: #b45309;
    }
</style>

<BitAiRendererBoundary Style="padding: 0.5rem; border-radius: 0.5rem; background-color: #fef2f2">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>

<BitAiRendererBoundary Class="custom-boundary">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>

<BitAiRendererBoundary Styles="@(new() { Notice = new() { Root = "border-style: dashed", Text = "font-style: italic" } })">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>

<BitAiRendererBoundary Classes="@(new() { Root = "custom-boundary-root", Notice = new() { Icon = "custom-boundary-icon" } })">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>

<div style="--bit-Tag-radius: 0; --bit-Tag-font-size: 1rem; --bit-Tag-min-height: 2.5rem;">
    <BitAiRendererBoundary>
        <WeatherCard Json="@unexpectedForecast" />
    </BitAiRendererBoundary>
</div>
""";

    private readonly string example5RazorCode = """
<BitAiRendererBoundary Dir="BitDir.Rtl" Label="کارت آب و هوا نتوانست این پیش‌بینی را نشان دهد">
    <WeatherCard Json="@unexpectedForecast" />
</BitAiRendererBoundary>
""";
}
