namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// What the E2E tests of BitStepper share: the locators of the parts of a stepper by its id, and the tolerance its
/// layout is measured with.
/// </summary>
internal static class StepperE2EHelpers
{
    /// <summary>
    /// The items of the stepper's list: its steps, and the hidden item holding the markers of its options, if any.
    /// </summary>
    public static ILocator StepperSteps(this IPage page, string id) => page.Locator($"#{id} > li");

    public static ILocator StepperIndicators(this IPage page, string id) => page.Locator($"#{id} .bit-stp-ind");

    public static ILocator StepperButtons(this IPage page, string id) => page.Locator($"#{id} .bit-stp-btn");

    public static ILocator StepperConnectors(this IPage page, string id) => page.Locator($"#{id} .bit-stp-con");

    /// <summary>
    /// Asserts that a measured length or position is within <paramref name="tolerance"/> pixels of the expected one.
    /// </summary>
    public static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < tolerance, $"{what}: expected about {expected}, got {actual}.");
    }
}
