namespace Bit.Cli.Projects;

public sealed record ProjectParts
{
    public static ProjectParts None { get; } = new();

    public string? ServerApi { get; init; }

    public IReadOnlyList<string> ServerWeb { get; init; } = [];

    public string? AppHost { get; init; }

    public string? ClientCore { get; init; }

    public IReadOnlyList<string> Web { get; init; } = [];

    public string? Maui { get; init; }

    public string? Windows { get; init; }

    public string? Workbench { get; init; }

    public string? LandingPage { get; init; }

    public IReadOnlyList<string> WellKnown { get; init; } = [];

    public bool IsEmpty => ServerApi is null && ServerWeb.Count == 0 && AppHost is null && ClientCore is null && Web.Count == 0 && Maui is null && Windows is null && Workbench is null && LandingPage is null && WellKnown.Count == 0;
}
