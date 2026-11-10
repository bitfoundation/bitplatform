namespace Bit.Bheadless;

/// <summary>
/// Library-wide settings. Register them for the whole app with
/// <c>builder.Services.AddBitBheadlessServices(configureOptions: options =&gt; …)</c>, or cascade an instance to a part of the
/// page (<c>&lt;CascadingValue Value="options" IsFixed="true"&gt;</c>), which then takes precedence there.
/// </summary>
public sealed class BitBhOptions
{
    /// <summary>The settings used when none are registered or cascaded.</summary>
    internal static readonly BitBhOptions Defaults = new();

    /// <summary>
    /// Makes the components work under a Content-Security-Policy whose <c>style-src</c> has no
    /// <c>'unsafe-inline'</c>, which refuses every <c>style</c> attribute - in the markup and set from script alike.
    /// <para>
    /// The components then render no <c>style</c> attribute of their own. The few styles they need - a visually
    /// hidden element's clipping, a slider thumb's position, a progress bar's <c>--bit-bh-progress</c> - go in
    /// <c>data-bit-bh-style</c> instead, and the JS runtime applies them through the CSSOM
    /// (<c>element.style.setProperty</c>), which such a policy allows. A <c>style</c> you put on a component is
    /// still rendered as a <c>style</c> attribute: it is yours, and the policy decides about it.
    /// </para>
    /// <para>
    /// The runtime starts applying those styles once it has loaded. For prerendered pages, add
    /// <c>&lt;script src="_content/Bit.Bheadless/bit-bheadless-csp.js"&gt;&lt;/script&gt;</c> to the
    /// <c>&lt;head&gt;</c> (before any stylesheet that matters for the first paint) so they apply as the page is parsed,
    /// before it is first painted. Default: <c>false</c>.
    /// </para>
    /// <para>
    /// The runtime applies the <c>data-bit-bh-style</c> of every element on the page, as it cannot tell the components'
    /// from others. Markup that is not yours (user content rendered as a <c>MarkupString</c>) must not carry it: have the
    /// sanitizer that cleans such markup remove <c>data-bit-bh-style</c>, as it removes <c>style</c>.
    /// </para>
    /// </summary>
    public bool StrictCsp { get; set; }
}
