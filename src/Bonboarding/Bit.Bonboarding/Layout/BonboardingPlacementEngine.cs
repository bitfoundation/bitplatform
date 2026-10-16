namespace Bit.Bonboarding;

/// <summary>
/// Collision-aware popover positioning. Deterministic, allocation-light and completely
/// side-effect free, which is what makes it directly unit testable without a browser.
/// </summary>
/// <remarks>
/// The strategy is the one users expect from a good tooltip. Each candidate side is first slid
/// along the target's cross axis to stay on screen, and only then scored by how many pixels would
/// still be clipped. Shifting therefore beats flipping: a popover next to a target at the edge of
/// the window slides sideways rather than jumping to another side. When the requested side really
/// does not fit, the opposite side is tried, then the perpendicular ones, and if nothing fits the
/// least-bad candidate is pushed fully inside the viewport.
/// </remarks>
public static class BonboardingPlacementEngine
{
    /// <summary>Positions a popover against a target.</summary>
    public static BonboardingPlacementResult Place(in BonboardingPlacementRequest request)
    {
        var safe = request.Viewport.Inflate(-request.ViewportPadding);

        // A degenerate safe area (tiny windows, or measurement taken before layout) would make
        // every candidate score as a total miss, so fall back to the raw viewport.
        if (safe.Width <= 0 || safe.Height <= 0)
        {
            safe = request.Viewport;
        }

        if (!request.HasTarget || request.Preferred == BonboardingPlacement.Center || request.Target.IsEmpty)
        {
            return Centered(request, safe);
        }

        var preferred = Resolve(request.Preferred, request.Target, safe, request.RightToLeft);
        var candidates = BuildCandidates(preferred, request.Fallbacks, request.RightToLeft);

        BonboardingPlacementResult best = default;
        var bestScore = double.PositiveInfinity;
        var first = true;

        foreach (var candidate in candidates)
        {
            // Score each candidate as it would actually be rendered, i.e. after the cross-axis
            // shift that keeps it on screen. Sliding a popover sideways is far less disruptive than
            // moving it to another side, so a side that only fits once shifted still wins.
            var rect = ClampCross(Compute(candidate, request), candidate.Side, safe);
            var overflow = ClippedArea(rect, safe);

            if (overflow <= 0.01)
            {
                return Finish(request, rect, candidate, safe, usedPreferred: first, overflow: 0);
            }

            if (overflow < bestScore)
            {
                bestScore = overflow;
                best = Finish(request, rect, candidate, safe, usedPreferred: first, overflow);
            }

            first = false;
        }

        // Nothing fit outright: shift the least-bad candidate into the safe area. When the popover
        // is simply larger than the viewport we clamp to the start of the safe area so the top of
        // the content stays readable and the popover scrolls internally.
        var shifted = Shift(best.Popover, safe);
        return Finish(request, shifted, (best.Side, best.Align), safe, best.UsedPreferred, ClippedArea(shifted, safe));
    }

    private static BonboardingPlacementResult Centered(in BonboardingPlacementRequest request, BonboardingRect safe)
    {
        var width = Math.Min(request.Popover.Width, safe.Width);
        var height = Math.Min(request.Popover.Height, safe.Height);
        var rect = new BonboardingRect(
            safe.X + ((safe.Width - width) / 2),
            safe.Y + ((safe.Height - height) / 2),
            width,
            height);

        return new BonboardingPlacementResult
        {
            Popover = rect.Round(),
            Side = BonboardingPlacementSide.Center,
            Align = BonboardingPlacementAlign.Center,
            ShowArrow = false,
            ArrowOffset = 0,
            UsedPreferred = true,
            Overflow = 0,
        };
    }

    /// <summary>Turns <see cref="BonboardingPlacement.Auto"/> into a concrete side and normalises RTL alignment.</summary>
    private static (BonboardingPlacementSide Side, BonboardingPlacementAlign Align) Resolve(
        BonboardingPlacement placement, BonboardingRect target, BonboardingRect safe, bool rtl)
    {
        if (placement == BonboardingPlacement.Auto)
        {
            return (BestSide(target, safe), BonboardingPlacementAlign.Center);
        }

        var (side, align) = Split(placement);
        return (side, rtl && side is BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom ? Mirror(align) : align);
    }

    private static BonboardingPlacementSide BestSide(BonboardingRect target, BonboardingRect safe)
    {
        // Free space on each side of the target, inside the safe area.
        var below = safe.Bottom - target.Bottom;
        var above = target.Top - safe.Top;
        var right = safe.Right - target.Right;
        var left = target.Left - safe.Left;

        // Ties favour bottom, where users look first, then top, then the inline sides.
        var bestValue = below;
        var bestSide = BonboardingPlacementSide.Bottom;

        if (above > bestValue) { bestValue = above; bestSide = BonboardingPlacementSide.Top; }
        if (right > bestValue) { bestValue = right; bestSide = BonboardingPlacementSide.Right; }
        if (left > bestValue) { bestSide = BonboardingPlacementSide.Left; }

        return bestSide;
    }

    private static List<(BonboardingPlacementSide Side, BonboardingPlacementAlign Align)> BuildCandidates(
        (BonboardingPlacementSide Side, BonboardingPlacementAlign Align) preferred,
        IReadOnlyList<BonboardingPlacement>? explicitFallbacks,
        bool rtl)
    {
        var list = new List<(BonboardingPlacementSide, BonboardingPlacementAlign)>(8) { preferred };

        if (explicitFallbacks is { Count: > 0 })
        {
            foreach (var fallback in explicitFallbacks)
            {
                if (fallback is BonboardingPlacement.Auto or BonboardingPlacement.Center) continue;
                var (side, align) = Split(fallback);
                if (rtl && side is BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom) align = Mirror(align);
                AddDistinct(list, (side, align));
            }

            return list;
        }

        // The opposite side keeps the same axis, which usually preserves the layout best.
        AddDistinct(list, (Opposite(preferred.Side), preferred.Align));

        foreach (var side in Perpendicular(preferred.Side))
        {
            AddDistinct(list, (side, BonboardingPlacementAlign.Center));
            AddDistinct(list, (side, BonboardingPlacementAlign.Start));
        }

        // Re-try the preferred axis with the other alignments before giving up on it.
        AddDistinct(list, (preferred.Side, BonboardingPlacementAlign.Start));
        AddDistinct(list, (preferred.Side, BonboardingPlacementAlign.End));

        return list;

        static void AddDistinct(List<(BonboardingPlacementSide, BonboardingPlacementAlign)> target, (BonboardingPlacementSide, BonboardingPlacementAlign) value)
        {
            if (!target.Contains(value)) target.Add(value);
        }
    }

    private static BonboardingRect Compute((BonboardingPlacementSide Side, BonboardingPlacementAlign Align) placement, in BonboardingPlacementRequest request)
    {
        var target = request.Target;
        var size = request.Popover;
        var gap = request.Offset;

        double x, y;

        switch (placement.Side)
        {
            case BonboardingPlacementSide.Top:
                y = target.Top - gap - size.Height;
                x = AlignAlong(target.Left, target.Width, size.Width, placement.Align);
                break;
            case BonboardingPlacementSide.Bottom:
                y = target.Bottom + gap;
                x = AlignAlong(target.Left, target.Width, size.Width, placement.Align);
                break;
            case BonboardingPlacementSide.Left:
                x = target.Left - gap - size.Width;
                y = AlignAlong(target.Top, target.Height, size.Height, placement.Align);
                break;
            case BonboardingPlacementSide.Right:
                x = target.Right + gap;
                y = AlignAlong(target.Top, target.Height, size.Height, placement.Align);
                break;
            default:
                x = target.CenterX - (size.Width / 2);
                y = target.CenterY - (size.Height / 2);
                break;
        }

        return new BonboardingRect(x, y, size.Width, size.Height);
    }

    private static double AlignAlong(double start, double extent, double size, BonboardingPlacementAlign align) => align switch
    {
        BonboardingPlacementAlign.Start => start,
        BonboardingPlacementAlign.End => start + extent - size,
        _ => start + ((extent - size) / 2),
    };

    /// <summary>Area of the popover falling outside the safe box; the candidate score.</summary>
    private static double ClippedArea(BonboardingRect rect, BonboardingRect safe)
    {
        var area = rect.Area;
        if (area <= 0) return 0;
        return Math.Max(0, area - rect.Intersect(safe).Area);
    }

    /// <summary>Slides a rectangle back inside the safe box without resizing it.</summary>
    private static BonboardingRect Shift(BonboardingRect rect, BonboardingRect safe)
    {
        var x = rect.Width <= safe.Width
            ? Math.Clamp(rect.X, safe.Left, safe.Right - rect.Width)
            : safe.Left;

        var y = rect.Height <= safe.Height
            ? Math.Clamp(rect.Y, safe.Top, safe.Bottom - rect.Height)
            : safe.Top;

        return rect with { X = x, Y = y };
    }

    /// <summary>
    /// Slides a candidate along the target's cross axis so it stays inside the safe area. The
    /// main-axis position is never touched, because that is what keeps the arrow pointing at the
    /// target and the popover clear of it.
    /// </summary>
    private static BonboardingRect ClampCross(BonboardingRect rect, BonboardingPlacementSide side, BonboardingRect safe) => side switch
    {
        BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom => rect with
        {
            X = rect.Width <= safe.Width ? Math.Clamp(rect.X, safe.Left, safe.Right - rect.Width) : safe.Left,
        },
        BonboardingPlacementSide.Left or BonboardingPlacementSide.Right => rect with
        {
            Y = rect.Height <= safe.Height ? Math.Clamp(rect.Y, safe.Top, safe.Bottom - rect.Height) : safe.Top,
        },
        _ => rect,
    };

    private static BonboardingPlacementResult Finish(
        in BonboardingPlacementRequest request,
        BonboardingRect rect,
        (BonboardingPlacementSide Side, BonboardingPlacementAlign Align) placement,
        BonboardingRect safe,
        bool usedPreferred,
        double overflow)
    {
        var adjusted = ClampCross(rect, placement.Side, safe);

        var (showArrow, arrowOffset) = Arrow(request, adjusted, placement.Side);

        return new BonboardingPlacementResult
        {
            Popover = adjusted.Round(),
            Side = placement.Side,
            Align = placement.Align,
            ShowArrow = showArrow,
            ArrowOffset = Math.Round(arrowOffset, 2),
            UsedPreferred = usedPreferred,
            Overflow = Math.Round(overflow, 2),
        };
    }

    private static (bool Show, double Offset) Arrow(in BonboardingPlacementRequest request, BonboardingRect popover, BonboardingPlacementSide side)
    {
        if (request.ArrowSize <= 0 || side == BonboardingPlacementSide.Center) return (false, 0);

        var target = request.Target;
        var horizontal = side is BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom;

        var popoverStart = horizontal ? popover.Left : popover.Top;
        var popoverEnd = horizontal ? popover.Right : popover.Bottom;
        var targetStart = horizontal ? target.Left : target.Top;
        var targetEnd = horizontal ? target.Right : target.Bottom;
        var targetCenter = horizontal ? target.CenterX : target.CenterY;

        // No cross-axis overlap means an arrow would point at empty space.
        if (targetEnd <= popoverStart || targetStart >= popoverEnd) return (false, 0);

        var inset = request.CornerRadius + (request.ArrowSize / 2);
        var min = popoverStart + inset;
        var max = popoverEnd - inset;
        if (max < min) return (false, 0);

        // Aim at the target centre, but never let the arrow leave the popover corners or slide off
        // the end of a target that is narrower than the corner inset.
        var center = Math.Clamp(targetCenter, min, max);
        center = Math.Clamp(center, Math.Min(targetStart, max), Math.Max(targetEnd, min));
        center = Math.Clamp(center, min, max);

        return (true, center - popoverStart);
    }

    internal static (BonboardingPlacementSide Side, BonboardingPlacementAlign Align) Split(BonboardingPlacement placement) => placement switch
    {
        BonboardingPlacement.Top => (BonboardingPlacementSide.Top, BonboardingPlacementAlign.Center),
        BonboardingPlacement.TopStart => (BonboardingPlacementSide.Top, BonboardingPlacementAlign.Start),
        BonboardingPlacement.TopEnd => (BonboardingPlacementSide.Top, BonboardingPlacementAlign.End),
        BonboardingPlacement.Bottom => (BonboardingPlacementSide.Bottom, BonboardingPlacementAlign.Center),
        BonboardingPlacement.BottomStart => (BonboardingPlacementSide.Bottom, BonboardingPlacementAlign.Start),
        BonboardingPlacement.BottomEnd => (BonboardingPlacementSide.Bottom, BonboardingPlacementAlign.End),
        BonboardingPlacement.Left => (BonboardingPlacementSide.Left, BonboardingPlacementAlign.Center),
        BonboardingPlacement.LeftStart => (BonboardingPlacementSide.Left, BonboardingPlacementAlign.Start),
        BonboardingPlacement.LeftEnd => (BonboardingPlacementSide.Left, BonboardingPlacementAlign.End),
        BonboardingPlacement.Right => (BonboardingPlacementSide.Right, BonboardingPlacementAlign.Center),
        BonboardingPlacement.RightStart => (BonboardingPlacementSide.Right, BonboardingPlacementAlign.Start),
        BonboardingPlacement.RightEnd => (BonboardingPlacementSide.Right, BonboardingPlacementAlign.End),
        BonboardingPlacement.Center => (BonboardingPlacementSide.Center, BonboardingPlacementAlign.Center),
        _ => (BonboardingPlacementSide.Bottom, BonboardingPlacementAlign.Center),
    };

    private static BonboardingPlacementAlign Mirror(BonboardingPlacementAlign align) => align switch
    {
        BonboardingPlacementAlign.Start => BonboardingPlacementAlign.End,
        BonboardingPlacementAlign.End => BonboardingPlacementAlign.Start,
        _ => BonboardingPlacementAlign.Center,
    };

    private static BonboardingPlacementSide Opposite(BonboardingPlacementSide side) => side switch
    {
        BonboardingPlacementSide.Top => BonboardingPlacementSide.Bottom,
        BonboardingPlacementSide.Bottom => BonboardingPlacementSide.Top,
        BonboardingPlacementSide.Left => BonboardingPlacementSide.Right,
        BonboardingPlacementSide.Right => BonboardingPlacementSide.Left,
        _ => BonboardingPlacementSide.Center,
    };

    private static BonboardingPlacementSide[] Perpendicular(BonboardingPlacementSide side) => side switch
    {
        BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom => [BonboardingPlacementSide.Right, BonboardingPlacementSide.Left],
        BonboardingPlacementSide.Left or BonboardingPlacementSide.Right => [BonboardingPlacementSide.Bottom, BonboardingPlacementSide.Top],
        _ => [BonboardingPlacementSide.Bottom, BonboardingPlacementSide.Top],
    };
}
