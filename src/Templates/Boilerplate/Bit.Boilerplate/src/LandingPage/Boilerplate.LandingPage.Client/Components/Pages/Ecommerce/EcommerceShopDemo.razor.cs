namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public partial class EcommerceShopDemo
{
    private static readonly (string Key, string Color)[] Pots =
    [
        (nameof(EcommerceStrings.DemoPotClay), "var(--lp-clay)"),
        (nameof(EcommerceStrings.DemoPotSage), "var(--lp-sage)"),
        (nameof(EcommerceStrings.DemoPotCharcoal), "var(--lp-charcoal)")
    ];

    private static readonly (string Key, int Price, double Scale)[] Sizes =
    [
        (nameof(EcommerceStrings.DemoSizeS), 24, .78),
        (nameof(EcommerceStrings.DemoSizeM), 48, 1),
        (nameof(EcommerceStrings.DemoSizeL), 89, 1.16)
    ];

    [AutoInject] private IStringLocalizer<EcommerceStrings> E = default!;

    private string? pot = Pots[0].Key;
    private string? size = Sizes[1].Key;
    private int bagCount;
    private BitChoiceGroupItem<string>[] potItems = [];
    private BitChoiceGroupItem<string>[] sizeItems = [];

    private (string Key, string Color) SelectedPot => Pots.FirstOrDefault(p => p.Key == pot, Pots[0]);

    private (string Key, int Price, double Scale) SelectedSize => Sizes.FirstOrDefault(s => s.Key == size, Sizes[1]);

    protected override void OnParametersSet()
    {
        potItems = [.. Pots.Select(p => new BitChoiceGroupItem<string> { Value = p.Key, Text = E[p.Key], AriaLabel = E[p.Key] })];
        sizeItems = [.. Sizes.Select(s => new BitChoiceGroupItem<string> { Value = s.Key, Text = E[s.Key] })];

        base.OnParametersSet();
    }

    private void Add() => bagCount++;
}
