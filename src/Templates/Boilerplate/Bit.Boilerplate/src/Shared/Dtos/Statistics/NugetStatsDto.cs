namespace Boilerplate.Shared.Dtos.Statistics;

public record NugetStatsDto(
    [property: JsonPropertyName("data")] IReadOnlyList<Datum> Data
);

public record Datum(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("totalDownloads")] int TotalDownloads
);

