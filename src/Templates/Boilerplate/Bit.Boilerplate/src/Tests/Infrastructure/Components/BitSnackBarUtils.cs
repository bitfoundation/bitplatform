namespace Boilerplate.Tests.Infrastructure.Components;

/// <summary>
/// Helpers for asserting on the Bit.BlazorUI <c>BitSnackBar</c> (the app renders one in its layout, see
/// <c>AppSnackBar</c>) in Playwright UI tests.
/// </summary>
public static class BitSnackBarUtils
{
    /// <summary>
    /// The snack bar item - the notification the user actually sees - whose text contains <paramref name="text"/>.
    /// <para>
    /// Scoping matters here, and <c>page.GetByText(text)</c> is the wrong locator for a snack bar: the component
    /// announces an arriving notification by copying its text into one of two visually hidden ARIA live regions
    /// (<c>.bit-snb-lvr</c>) that sit alongside the items. That copy is not decoration - a live region only announces
    /// text that arrives INSIDE a region the screen reader was already watching, so an element that shows up with its
    /// text already in it is silent, and the text has to be written into the waiting region instead. The consequence
    /// for a test is that the message is on the page TWICE for as long as the announcement stands, and an unscoped
    /// locator resolves to two elements and fails with a strict mode violation rather than finding the snack bar.
    /// </para>
    /// <para>
    /// The live regions are siblings of the items rather than children, so scoping to the item element excludes them
    /// while still matching a message that lands in the title or in the body.
    /// </para>
    /// </summary>
    public static ILocator GetSnackBar(IPage page, string text) => page.Locator(".bit-snb-itm", new() { HasText = text });
}
