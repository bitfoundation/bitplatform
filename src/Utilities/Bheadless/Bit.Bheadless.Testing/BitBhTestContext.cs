using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A bUnit test context prepared for Bheadless components. Derive your test
/// classes from it instead of <see cref="BunitContext"/>.
/// </summary>
/// <remarks>
/// <para>On top of <see cref="BitBhTestingExtensions.AddBitBheadlessServices"/> (loose JS interop,
/// library services, a recording <see cref="Announcer"/>), it emulates the part of the
/// browser runtime that affects behaviour rather than visuals: item collections
/// (options, menu items, tree items, roving items …) learn their <b>document order</b>
/// and <b>rendered text</b> from the rendered markup, exactly as they do from the real
/// DOM. Typeahead, text-based filtering and arrow-key order then work as in a browser.</para>
/// <para>Focus is not emulated; assert focus moves with <see cref="BitBhTestingExtensions.LastFocusRequest"/>.</para>
/// </remarks>
public abstract class BitBhTestContext : BunitContext
{
    private readonly BitBhBrowserEmulation _browser;

    /// <summary>Creates the context.</summary>
    protected BitBhTestContext()
    {
        Announcer = this.AddBitBheadlessServices();
        _browser = new BitBhBrowserEmulation(this);
    }

    /// <summary>Records live-region announcements.</summary>
    public BitBhTestAnnouncer Announcer { get; }

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        => _browser.Track(() => base.Render(parameterBuilder));

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(RenderFragment renderFragment)
        => _browser.Track(() => base.Render<TComponent>(renderFragment));

    /// <inheritdoc />
    public override IRenderedComponent<ContainerFragment> Render(RenderFragment renderFragment)
        => _browser.Track(() => base.Render(renderFragment));
}
