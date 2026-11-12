namespace Bit.Bonboarding.Tests;

/// <summary>
/// The placement engine over thousands of generated layouts - every placement, both directions, phone to desktop
/// viewports, targets at the edges and larger than the screen - checked against the promises it makes whatever the
/// layout. The seed is fixed, so a failure names a layout that can be replayed.
/// </summary>
[TestClass]
public class PlacementInvariantTests
{
    private const int Layouts = 4000;
    private const double Tolerance = 0.01;

    private static readonly BonboardingPlacement[] Placements = Enum.GetValues<BonboardingPlacement>();

    private static IEnumerable<BonboardingPlacementRequest> GenerateLayouts(int seed)
    {
        var random = new Random(seed);

        for (var i = 0; i < Layouts; i++)
        {
            // Phone portrait to wide desktop, sometimes with a visual viewport that does not start at the origin
            // (pinch zoom, a virtual keyboard).
            var viewport = new BonboardingRect(
                random.Next(4) == 0 ? random.Next(0, 300) : 0,
                random.Next(4) == 0 ? random.Next(0, 300) : 0,
                random.Next(320, 2560),
                random.Next(400, 1440));

            // Mostly small controls, some panels larger than the screen, some partly or wholly off it.
            var width = random.Next(10) == 0 ? viewport.Width * (0.5 + random.NextDouble()) : random.Next(8, 400);
            var height = random.Next(10) == 0 ? viewport.Height * (0.5 + random.NextDouble()) : random.Next(8, 200);
            var target = new BonboardingRect(
                viewport.X + (random.NextDouble() * (viewport.Width + 200)) - 100,
                viewport.Y + (random.NextDouble() * (viewport.Height + 200)) - 100,
                width,
                height);

            var popover = new BonboardingSize(random.Next(160, 520), random.Next(80, 480));

            yield return new BonboardingPlacementRequest
            {
                Target = target,
                Popover = popover,
                Viewport = viewport,
                Preferred = Placements[random.Next(Placements.Length)],
                Offset = random.Next(0, 24),
                ViewportPadding = random.Next(0, 32),
                ArrowSize = random.Next(3) == 0 ? 0 : random.Next(6, 16),
                CornerRadius = random.Next(0, 20),
                RightToLeft = random.Next(2) == 0,
                HasTarget = true,
            };
        }
    }

    private static BonboardingRect Safe(in BonboardingPlacementRequest request)
    {
        var safe = request.Viewport.Inflate(-request.ViewportPadding);
        return safe.Width <= 0 || safe.Height <= 0 ? request.Viewport : safe;
    }

    private static string Describe(in BonboardingPlacementRequest request, in BonboardingPlacementResult result)
        => $"target {request.Target}, popover {request.Popover.Width}x{request.Popover.Height}, viewport {request.Viewport}, "
           + $"{request.Preferred}{(request.RightToLeft ? " rtl" : "")}, padding {request.ViewportPadding}, offset {request.Offset} -> {result}";

    [TestMethod]
    [DataRow(1)]
    [DataRow(20261111)]
    public void A_Popover_That_Fits_The_Screen_Is_Always_Placed_Wholly_On_It(int seed)
    {
        foreach (var request in GenerateLayouts(seed))
        {
            var safe = Safe(request);
            if (request.Popover.Width > safe.Width || request.Popover.Height > safe.Height) continue;

            var result = BonboardingPlacementEngine.Place(request);
            var popover = result.Popover;

            Assert.IsTrue(
                popover.Left >= safe.Left - Tolerance && popover.Top >= safe.Top - Tolerance
                && popover.Right <= safe.Right + Tolerance && popover.Bottom <= safe.Bottom + Tolerance,
                Describe(request, result));

            Assert.AreEqual(request.Popover.Width, popover.Width, Tolerance, "The engine moves the popover; it never resizes one that fits.");
            Assert.AreEqual(request.Popover.Height, popover.Height, Tolerance);
        }
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(424242)]
    public void When_Any_Side_Has_Room_The_Popover_Never_Covers_Its_Target(int seed)
    {
        var checkedLayouts = 0;

        foreach (var request in GenerateLayouts(seed))
        {
            if (request.Preferred == BonboardingPlacement.Center) continue;

            // Worked out here rather than read from the result: whether the popover fits beside the target on some
            // side of the screen's safe area. Only when no side does may the engine fall back to covering it.
            var safe = Safe(request);
            var (target, popover, gap) = (request.Target, request.Popover, request.Offset);
            var fitsAcross = popover.Width <= safe.Width;
            var fitsDown = popover.Height <= safe.Height;

            var hasRoom = (fitsAcross && target.Top - gap - safe.Top >= popover.Height)
                || (fitsAcross && safe.Bottom - target.Bottom - gap >= popover.Height)
                || (fitsDown && target.Left - gap - safe.Left >= popover.Width)
                || (fitsDown && safe.Right - target.Right - gap >= popover.Width);

            if (!hasRoom) continue;

            var result = BonboardingPlacementEngine.Place(request);
            var overlap = result.Popover.Intersect(target);

            Assert.IsTrue(overlap.Width <= Tolerance || overlap.Height <= Tolerance, Describe(request, result));
            checkedLayouts++;
        }

        Assert.IsGreaterThan(Layouts / 2, checkedLayouts, "Most generated layouts leave room on some side.");
    }

    [TestMethod]
    [DataRow(3)]
    public void The_Arrow_Stays_On_The_Popovers_Edge_And_Clear_Of_Its_Corners(int seed)
    {
        foreach (var request in GenerateLayouts(seed))
        {
            var result = BonboardingPlacementEngine.Place(request);
            if (!result.ShowArrow) continue;

            Assert.AreNotEqual(BonboardingPlacementSide.Center, result.Side, Describe(request, result));

            var edge = result.Side is BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom ? result.Popover.Width : result.Popover.Height;
            Assert.IsTrue(result.ArrowOffset >= -Tolerance && result.ArrowOffset <= edge + Tolerance, Describe(request, result));
        }
    }

    [TestMethod]
    public void Placement_Is_Deterministic_And_Always_Finite()
    {
        foreach (var request in GenerateLayouts(5).Take(500))
        {
            var first = BonboardingPlacementEngine.Place(request);
            var second = BonboardingPlacementEngine.Place(request);

            Assert.AreEqual(first, second, Describe(request, first));
            Assert.IsTrue(double.IsFinite(first.Popover.X) && double.IsFinite(first.Popover.Y) && double.IsFinite(first.ArrowOffset), Describe(request, first));
        }
    }

    [TestMethod]
    public void Measurements_Taken_Before_Layout_Do_Not_Break_Placement()
    {
        // Zero viewports and zero popovers are what the browser reports before anything has rendered.
        BonboardingPlacementRequest[] degenerate =
        [
            new() { Target = new(10, 10, 50, 20), Popover = new(300, 200), Viewport = BonboardingRect.Empty, HasTarget = true, ViewportPadding = 16 },
            new() { Target = new(10, 10, 50, 20), Popover = new(0, 0), Viewport = new(0, 0, 800, 600), HasTarget = true },
            new() { Target = new(10, 10, 0, 0), Popover = new(300, 200), Viewport = new(0, 0, 800, 600), HasTarget = true },
            new() { Target = new(10, 10, 50, 20), Popover = new(300, 200), Viewport = new(0, 0, 20, 20), HasTarget = true, ViewportPadding = 16 },
        ];

        foreach (var request in degenerate)
        {
            var result = BonboardingPlacementEngine.Place(request);
            Assert.IsTrue(double.IsFinite(result.Popover.X) && double.IsFinite(result.Popover.Y), Describe(request, result));
        }
    }
}
