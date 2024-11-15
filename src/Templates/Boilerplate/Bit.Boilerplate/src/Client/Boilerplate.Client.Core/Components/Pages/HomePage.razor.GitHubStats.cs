namespace Boilerplate.Client.Core.Components.Pages;


public record GitHubStats(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("homepage")] string Homepage,
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("stargazers_count")] int StargazersCount,
    [property: JsonPropertyName("watchers_count")] int WatchersCount,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("forks_count")] int ForksCount,
    [property: JsonPropertyName("open_issues_count")] int OpenIssuesCount,
    [property: JsonPropertyName("topics")] IReadOnlyList<string> Topics,
    [property: JsonPropertyName("default_branch")] string DefaultBranch,
    [property: JsonPropertyName("network_count")] int NetworkCount,
    [property: JsonPropertyName("subscribers_count")] int SubscribersCount
);


