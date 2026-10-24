using Bit.Blex;
using Bit.Blex.Samples.Core.Services;

namespace Bit.Blex.Samples.Core.Stores;

/// <summary>
/// Demonstrates cancellable effects: <c>[BlexEffectAttribute(Concurrency = Latest)]</c> gives type-ahead
/// ("switchMap") semantics -- a new <c>Load</c> cancels the previous one -- and the trailing
/// <see cref="CancellationToken"/> parameter makes the generator emit a <c>CancelLoad()</c> method.
/// Loading and error state (<c>LoadIsLoading</c>/<c>LoadError</c>) are generated; cancellations
/// are never surfaced as errors. Stores are plain DI services, so injecting the weather service just works.
/// </summary>
[BlexStoreAttribute(Name = "weather")]
public partial class WeatherStore
{
    private readonly WeatherService _weather;

    public WeatherStore(WeatherService weather) => _weather = weather;

    [BlexStateAttribute] private WeatherForecast[]? _forecasts;
    [BlexStateAttribute] private DateTimeOffset? _loadedAt;

    [BlexComputedAttribute]
    private int ComputeCount() => Forecasts?.Length ?? 0;

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnLoad(bool simulateFailure, CancellationToken ct)
    {
        Forecasts = null;
        LoadedAt = null;

        Forecasts = await _weather.GetForecastsAsync(simulateFailure, ct);
        LoadedAt = DateTimeOffset.Now;
    }

    [BlexActionAttribute]
    private void OnClear()
    {
        Forecasts = null;
        LoadedAt = null;
    }
}
