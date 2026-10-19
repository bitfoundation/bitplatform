using System.Globalization;
using Bit.Bonboarding.Demo.Server.Dtos;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// Runs Bit.Bonboarding's own placement engine on a set of measurements, so "where will the popover
/// go?" is answered by the code that decides it at runtime rather than by a guess about it.
/// <para>
/// The request is built exactly the way a running tour builds it: the target is grown by the
/// spotlight padding first - the popover is positioned against the spotlight, not the bare element -
/// and the popover's corner radius is the one the default stylesheet draws, which is what keeps the
/// arrow out of the corners.
/// </para>
/// </summary>
public static class BonboardingPlacementLab
{
    /// <summary>The corner radius the running library hands the engine; see BonboardingTourSession.</summary>
    private const double CornerRadius = 12;

    public static readonly string[] PlacementNames = Enum.GetNames<BonboardingPlacement>();

    /// <summary>Parses a placement name such as "BottomStart", "bottom-start" or "BonboardingPlacement.Bottom".</summary>
    public static BonboardingPlacement? ParsePlacement(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return BonboardingPlacement.Auto;

        var text = value.Trim();
        var dot = text.LastIndexOf('.');
        if (dot >= 0) text = text[(dot + 1)..];

        text = text.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse<BonboardingPlacement>(text, ignoreCase: true, out var placement) && Enum.IsDefined(placement)
            ? placement
            : null;
    }

    public static BonboardingPlacementAnalysisDto Analyze(
        BonboardingRect target,
        BonboardingSize popover,
        BonboardingRect viewport,
        BonboardingPlacement placement,
        IReadOnlyList<BonboardingPlacement>? fallbacks,
        double offset,
        double viewportPadding,
        double arrowSize,
        double spotlightPadding,
        bool rightToLeft,
        bool hasTarget)
    {
        var spotlight = hasTarget && target.IsEmpty is false ? target.Inflate(spotlightPadding) : BonboardingRect.Empty;
        var targetPlaced = spotlight.IsEmpty is false;

        var result = BonboardingPlacementEngine.Place(new BonboardingPlacementRequest
        {
            Target = spotlight,
            Popover = popover,
            Viewport = viewport,
            Preferred = placement,
            Fallbacks = fallbacks,
            Offset = offset,
            ViewportPadding = viewportPadding,
            ArrowSize = arrowSize,
            CornerRadius = CornerRadius,
            RightToLeft = rightToLeft,
            HasTarget = targetPlaced,
        });

        return new BonboardingPlacementAnalysisDto
        {
            Requested = placement.ToString(),
            Side = result.Side.ToString(),
            Align = result.Align.ToString(),
            Popover = ToDto(result.Popover),
            Spotlight = targetPlaced ? ToDto(spotlight.Round()) : null,
            ShowArrow = result.ShowArrow,
            ArrowOffset = result.ArrowOffset,
            UsedPreferred = result.UsedPreferred,
            Overflow = result.Overflow,
            Notes = Explain(result, placement, fallbacks, popover, viewport, viewportPadding, targetPlaced, hasTarget)
        };
    }

    private static string[] Explain(
        BonboardingPlacementResult result,
        BonboardingPlacement placement,
        IReadOnlyList<BonboardingPlacement>? fallbacks,
        BonboardingSize popover,
        BonboardingRect viewport,
        double viewportPadding,
        bool targetPlaced,
        bool hasTarget)
    {
        var notes = new List<string>();

        if (targetPlaced is false)
        {
            notes.Add(hasTarget
                ? "The target has no area, so the step is shown as a centred card - the same thing a running tour does for a step whose element was not found and MissingTarget is Center."
                : "No target: the step is a centred card with no arrow and no spotlight.");
        }
        else if (placement == BonboardingPlacement.Center)
        {
            notes.Add("Center ignores the target and centres the popover in the viewport, while the spotlight still highlights the element.");
        }
        else if (placement == BonboardingPlacement.Auto && result.UsedPreferred)
        {
            notes.Add($"Auto chose {result.Side}: the side of the spotlight with the most free space inside the viewport padding, ties going to Bottom, then Top, then the inline sides.");
        }
        else if (result.UsedPreferred)
        {
            notes.Add($"The requested {placement} fitted, after any sliding along the target's cross axis that kept it on screen.");
        }
        else
        {
            var requested = placement == BonboardingPlacement.Auto ? "The side Auto picked" : placement.ToString();

            notes.Add(fallbacks is { Count: > 0 }
                ? $"{requested} did not fit, so the explicit PlacementFallbacks were tried in order and {result.Side}/{result.Align} was the first that fitted (or the least-clipped one)."
                : $"{requested} did not fit even after sliding, so the engine flipped: the opposite side first, then the perpendicular ones. {result.Side}/{result.Align} won. Set PlacementFallbacks to choose that order yourself.");
        }

        if (result.Overflow > 0)
        {
            var safeWidth = viewport.Width - (2 * viewportPadding);
            var safeHeight = viewport.Height - (2 * viewportPadding);

            notes.Add(popover.Width > safeWidth || popover.Height > safeHeight
                ? string.Create(CultureInfo.InvariantCulture, $"The popover ({popover.Width:0.##}x{popover.Height:0.##}) is larger than the area inside the viewport padding ({safeWidth:0.##}x{safeHeight:0.##}); it is pinned to the start of that area and its content scrolls internally (the stylesheet caps it with --bit-bonboarding-popover-max-height).")
                : string.Create(CultureInfo.InvariantCulture, $"{result.Overflow:0.##} square pixels could not be kept on screen on any side; the least-clipped candidate was shifted inside the viewport. A smaller popover, a smaller Offset or a smaller ViewportPadding makes room."));
        }

        if (targetPlaced && result.Side != BonboardingPlacementSide.Center && result.ShowArrow is false)
        {
            notes.Add("No arrow: either ArrowSize is 0, or the popover had to slide so far that it no longer overlaps the spotlight along its edge, so an arrow would point at nothing.");
        }

        return [.. notes];
    }

    private static BonboardingRectDto ToDto(BonboardingRect rect) => new()
    {
        X = rect.X,
        Y = rect.Y,
        Width = rect.Width,
        Height = rect.Height
    };
}
