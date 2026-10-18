namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>A source file of the demo/samples, retrievable through GetButilSourceFile.</summary>
public record ButilSourceFileDto
{
    /// <summary>The path to pass to GetButilSourceFile, e.g. "Demo/Client/Pages/ClipboardPage.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Demo (this documentation site) or Sample (the minimal hosting-model samples).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}
