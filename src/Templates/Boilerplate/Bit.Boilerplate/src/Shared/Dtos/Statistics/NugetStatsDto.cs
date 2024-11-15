namespace Boilerplate.Shared.Dtos.Statistics;

public record NugetStatsDto(
    [property: JsonPropertyName("data")] IReadOnlyList<Datum> Data
);

public record Datum(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("projectUrl")] string ProjectUrl,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("authors")] IReadOnlyList<string> Authors,
    [property: JsonPropertyName("owners")] IReadOnlyList<string> Owners,
    [property: JsonPropertyName("totalDownloads")] int TotalDownloads,
    [property: JsonPropertyName("versions")] IReadOnlyList<VersionRecord> Versions
);

public record VersionRecord(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("downloads")] int Downloads,
    [property: JsonPropertyName("@id")] string Id
);


