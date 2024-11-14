namespace Boilerplate.Shared.Dtos.Home;

public record NugetStatsDto(
    [property: JsonPropertyName("@context")] Context Context,
    [property: JsonPropertyName("totalHits")] int TotalHits,
    [property: JsonPropertyName("data")] IReadOnlyList<Datum> Data
);

public record Context(
    [property: JsonPropertyName("@vocab")] string Vocab,
    [property: JsonPropertyName("@base")] string Base
);

public record Datum(
    [property: JsonPropertyName("@id")] string _Id,
    [property: JsonPropertyName("@type")] string Type,
    [property: JsonPropertyName("registration")] string Registration,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("iconUrl")] string IconUrl,
    [property: JsonPropertyName("licenseUrl")] string LicenseUrl,
    [property: JsonPropertyName("projectUrl")] string ProjectUrl,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("authors")] IReadOnlyList<string> Authors,
    [property: JsonPropertyName("owners")] IReadOnlyList<string> Owners,
    [property: JsonPropertyName("totalDownloads")] int TotalDownloads,
    [property: JsonPropertyName("verified")] bool Verified,
    [property: JsonPropertyName("packageTypes")] IReadOnlyList<PackageType> PackageTypes,
    [property: JsonPropertyName("versions")] IReadOnlyList<VersionRecord> Versions,
    [property: JsonPropertyName("vulnerabilities")] IReadOnlyList<object> Vulnerabilities
);

public record PackageType(
    [property: JsonPropertyName("name")] string Name
);

public record VersionRecord(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("downloads")] int Downloads,
    [property: JsonPropertyName("@id")] string Id
);


