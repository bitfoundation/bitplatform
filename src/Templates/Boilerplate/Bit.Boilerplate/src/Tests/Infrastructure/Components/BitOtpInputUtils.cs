namespace Boilerplate.Tests.Infrastructure.Components;

/// <summary>
/// Playwright helpers for driving the Bit.BlazorUI <c>BitOtpInput</c> component (its boxes render as <c>.bit-otp-inp</c>) in UI tests.
/// </summary>
public static class BitOtpInputUtils
{
    /// <summary>
    /// Fills the currently visible <c>BitOtpInput</c> with <paramref name="code"/> in one shot by setting the whole
    /// code on its first box. <c>BitOtpInput</c> handles a single input event that carries more than one character by
    /// distributing the characters across all boxes itself (the same path it uses for a paste) and then firing its
    /// <c>OnFill</c>. So one <see cref="ILocator.FillAsync"/> - which sets the value programmatically (unconstrained by
    /// the boxes' per-box <c>maxlength</c>) and dispatches a single input event carrying the full code - fills every
    /// box in one atomic, timing-independent step. This avoids the per-box focus-advance that made filling each box
    /// separately flaky: that advance is an async Blazor round-trip, so at full speed the keys outran it and piled up
    /// on an already-filled box.
    /// </summary>
    public static async Task FillOtpInputs(IPage page, string code)
    {
        var firstInput = page.Locator(".bit-otp-inp").First;
        await firstInput.WaitForAsync();

        await firstInput.FillAsync(code);
    }
}
