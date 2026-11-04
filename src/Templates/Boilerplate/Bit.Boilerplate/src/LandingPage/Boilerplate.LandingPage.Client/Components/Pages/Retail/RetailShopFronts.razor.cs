namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailShopFronts
{
    private const string AwningStripes = "M22 40l-3 12M34 40l-2 12M46 40l-1 12M58 40v12M70 40l1 12M82 40l2 12M94 40l3 12";
    private const string AwningScallops = "M14 52a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 12 0a6 4 0 0 0 8 0Z";

    private static readonly (string Wall, string Awning, string Goods)[] looks =
    [
        ("#8E3B2A", "#C8331C", "#E5A13A"),
        ("#3F5A6E", "#FFD43B", "#7BAA4A"),
        ("#6B6F75", "#C8331C", "#D9542E"),
        ("#4E6B4F", "#F4F1EA", "#E5A13A"),
        ("#2F3A56", "#FFD43B", "#C8331C"),
        ("#7A5C45", "#1D7A4F", "#F2C14E")
    ];

    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    [Parameter] public string? Class { get; set; }
}
