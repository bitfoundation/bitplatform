namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>One heading of the library's README, which doubles as its reference guide.</summary>
public record ButilGuideSectionDto
{
    /// <summary>The heading text, e.g. "Subscriptions are disposable". Pass it to GetButilGuideSection.</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}
