using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// Goes to one slide of a <see cref="BitBhCarousel"/> (named "Go to slide 3"); the current one has <c>aria-current="true"</c>
/// and <c>data-current</c>. Render one per position: <c>@for (var i = 0; i &lt; c.PageCount; i++)</c>.
/// </summary>
public class BitBhCarouselIndicator : BitBhCarouselButtonBase
{
    /// <summary>The slide it goes to (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    protected override bool IsDisabled => false;

    protected override Task ActivateAsync(BitBhCarousel owner) => owner.GoToAsync(Index);

    protected override string Label => Strings.GoToSlide(Index);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var current = Owner?.CurrentIndex == Index;
        if (current) attrs["aria-current"] = "true";
        SetDataFlag(attrs, "current", current);
        SetDataValue(attrs, "index", Index.ToString(CultureInfo.InvariantCulture));
        return attrs;
    }
}
