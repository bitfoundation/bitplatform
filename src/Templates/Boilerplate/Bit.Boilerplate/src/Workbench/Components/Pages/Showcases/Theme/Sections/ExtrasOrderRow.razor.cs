namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// One order of the long lists of the Extras section (BitInfiniteScrolling and BitVirtualize). It is drawn from its
/// index alone, so a list of thousands holds nothing but numbers.
/// </summary>
public partial class ExtrasOrderRow
{
    /// <summary>The order's place in the list, newest first.</summary>
    [Parameter] public int Index { get; set; }

    /// <summary>Fills the fixed height a virtualized row is given, instead of sizing to its content.</summary>
    [Parameter] public bool Fixed { get; set; }

    private int Number => ExtrasSamples.LatestOrderNumber - Index;

    private (string NameKey, decimal Price) Product => ExtrasSamples.Products[Index % ExtrasSamples.Products.Length];

    private int Quantity => 1 + Index % 3;

    private decimal Amount => Product.Price * Quantity;

    private string ProductText => Localizer[Product.NameKey];

    /// <summary>An order every seven minutes, back from the samples' day.</summary>
    private string TimeText => ThemeSamples.Today.AddMinutes(-7 * Index).ToString("d MMM, HH:mm", CultureInfo.CurrentCulture);
}
