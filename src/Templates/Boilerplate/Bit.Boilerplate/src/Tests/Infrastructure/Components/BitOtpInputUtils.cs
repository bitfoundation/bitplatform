using Bunit;
using AngleSharp.Dom;

namespace Boilerplate.Tests.Infrastructure.Components;

/// <summary>
/// Helpers for driving the Bit.BlazorUI <c>BitOtpInput</c> component (its boxes render as <c>.bit-otp-inp</c>) in UI tests,
/// through a real browser (Playwright) and in-memory (bUnit) alike.
/// </summary>
public static class BitOtpInputUtils
{
    /// <summary>
    /// Fills the page's first <c>BitOtpInput</c> with <paramref name="code"/>. Only use this on a page that renders a
    /// single one - see the <see cref="FillOtpInputs(ILocator, string)"/> overload otherwise, because "first in the
    /// DOM" is not "the one the user is looking at": the settings page, for instance, renders the two-factor section's
    /// boxes well before the elevated access prompt's, so filling the first submits the code as a 2fa enable attempt
    /// and leaves the prompt unanswered.
    /// </summary>
    public static Task FillOtpInputs(IPage page, string code) => FillOtpInputs(page.Locator("body"), code);

    /// <summary>
    /// Fills the first <c>BitOtpInput</c> inside <paramref name="scope"/> with <paramref name="code"/> in one shot by
    /// setting the whole code on its first box. <c>BitOtpInput</c> handles a single input event that carries more than
    /// one character by distributing the characters across all boxes itself (the same path it uses for a paste) and
    /// then firing its <c>OnFill</c>. So one <see cref="ILocator.FillAsync"/> - which sets the value programmatically
    /// (unconstrained by the boxes' per-box <c>maxlength</c>) and dispatches a single input event carrying the full
    /// code - fills every box in one atomic, timing-independent step. This avoids the per-box focus-advance that made
    /// filling each box separately flaky: that advance is an async Blazor round-trip, so at full speed the keys outran
    /// it and piled up on an already-filled box.
    /// </summary>
    /// <param name="scope">The element that owns the intended input, e.g. the prompt modal it is rendered in.</param>
    public static async Task FillOtpInputs(ILocator scope, string code)
    {
        var firstInput = scope.Locator(".bit-otp-inp").First;
        await firstInput.WaitForAsync();

        await firstInput.FillAsync(code);
    }

    /// <summary>
    /// The bUnit counterpart of <see cref="FillOtpInputs(IPage, string)"/>: raises a single <c>input</c> event carrying
    /// the whole <paramref name="code"/> on the first box, which is the very same path the browser test drives.
    /// <c>BitOtpInput.HandleOnInput</c> sees a diff longer than one character, spreads it across the boxes (the path it
    /// shares with a paste) and then fires its <c>OnFill</c> - so the code lands and the panel submits in one step,
    /// with no per-box focus-advance to race.
    /// </summary>
    /// <param name="firstOtpInput">The first <c>.bit-otp-inp</c> element, e.g. <c>cut.Find(".bit-otp-inp")</c>.</param>
    public static void FillOtpInputs(IElement firstOtpInput, string code)
    {
        firstOtpInput.Input(code);
    }
}
