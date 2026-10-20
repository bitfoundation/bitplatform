using System.Net.Http.Json;
using Bit.Blex;

namespace Bit.Blex.Samples.Wasm.Stores;

/// <summary>A forecast row loaded from <c>sample-data/weather.json</c>.</summary>
public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>Fahrenheit projection of <see cref="TemperatureC"/>.</summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>
/// Demonstrates cancellable effects: <c>[BlexEffectAttribute(Concurrency = Latest)]</c> gives type-ahead
/// ("switchMap") semantics -- a new <c>Load</c> cancels the previous one -- and the trailing
/// <see cref="CancellationToken"/> parameter makes the generator emit a <c>CancelLoad()</c> method.
/// Loading and error state (<c>LoadIsLoading</c>/<c>LoadError</c>) are generated; cancellations
/// are never surfaced as errors. Stores are plain DI services, so injecting HttpClient just works.
/// </summary>
[BlexStoreAttribute(Name = "weather")]
public partial class WeatherStore
{
    private readonly HttpClient _http;

    public WeatherStore(HttpClient http) => _http = http;

    [BlexStateAttribute] private WeatherForecast[]? _forecasts;
    [BlexStateAttribute] private DateTimeOffset? _loadedAt;

    [BlexComputedAttribute]
    private int ComputeCount() => Forecasts?.Length ?? 0;

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnLoad(bool simulateFailure, CancellationToken ct)
    {
        Forecasts = null;
        LoadedAt = null;

        // Pretend the network is slow so cancellation and "latest wins" are observable.
        await Task.Delay(1500, ct);

        if (simulateFailure)
            throw new HttpRequestException("The weather service is unavailable (simulated).");

        Forecasts = await _http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json", BlexJson.Options, ct);
        LoadedAt = DateTimeOffset.Now;
    }

    [BlexActionAttribute]
    private void OnClear()
    {
        Forecasts = null;
        LoadedAt = null;
    }
}
