namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>The combined consequences of building a feature on a set of Butil APIs.</summary>
public record ButilFeaturePlanDto
{
    /// <summary>One entry per requested API, in the order they were passed.</summary>
    public required ButilApiInspectionDto[] Apis { get; init; }

    /// <summary>Names that matched nothing - a typo, or an API this library does not wrap.</summary>
    public required string[] Unknown { get; init; }

    /// <summary>True when at least one API only works over HTTPS or on localhost.</summary>
    public required bool RequiresSecureContext { get; init; }

    /// <summary>True when at least one API prompts the user, so a denial has to be a supported outcome.</summary>
    public required bool RequiresPermission { get; init; }

    /// <summary>True when at least one API only works from inside a click handler.</summary>
    public required bool RequiresUserGesture { get; init; }

    /// <summary>The APIs that do not work in every engine, each with the engines that do implement it.</summary>
    public required string[] EngineLimited { get; init; }

    /// <summary>The ordered checklist for shipping this feature.</summary>
    public required string[] Checklist { get; init; }

    /// <summary>Names past the per-plan cap, which were not inspected. Pass them in a second call.</summary>
    public string[]? Ignored { get; init; }
}
