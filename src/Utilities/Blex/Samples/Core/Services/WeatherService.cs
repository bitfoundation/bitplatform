namespace Bit.Blex.Samples.Core.Services;

/// <summary>A forecast row returned by <see cref="WeatherService"/>.</summary>
public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>Fahrenheit projection of <see cref="TemperatureC"/>.</summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>
/// Stands in for a weather API. The forecasts are generated rather than downloaded so every host runs
/// the same code: a server-side render has no base address to call back into, and a BlazorWebView's
/// app files are not reachable over HTTP at all.
/// </summary>
public sealed class WeatherService
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public async Task<WeatherForecast[]> GetForecastsAsync(bool simulateFailure, CancellationToken cancellationToken)
    {
        // Pretend the network is slow so cancellation and "latest wins" are observable.
        await Task.Delay(1500, cancellationToken);

        if (simulateFailure)
            throw new HttpRequestException("The weather service is unavailable (simulated).");

        var today = DateOnly.FromDateTime(DateTime.Now);

        return [.. Enumerable.Range(1, 5).Select(day => new WeatherForecast(
            today.AddDays(day),
            Random.Shared.Next(-20, 55),
            Summaries[Random.Shared.Next(Summaries.Length)]))];
    }
}
