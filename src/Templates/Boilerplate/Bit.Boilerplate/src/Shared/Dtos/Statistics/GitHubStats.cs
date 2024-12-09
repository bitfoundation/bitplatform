namespace Boilerplate.Shared.Dtos.Statistics;

public record GitHubStats(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stargazers_count")] int StargazersCount,
    [property: JsonPropertyName("forks_count")] int ForksCount
);
