namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailLoyalty
{
    private const int Balance = 320;
    private const int VoucherPoints = 500;
    private const decimal VoucherValue = 5;

    private static readonly (int X, int Width)[] bars = Bars();

    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private double weekly = 80;

    private int PointsPerYear => (int)Math.Round(weekly) * 52;

    private int Vouchers => PointsPerYear / VoucherPoints;

    private string Money(decimal amount) => R[nameof(RetailStrings.Money), amount.ToString(amount % 1 is 0 ? "0" : "0.00", CurrentCulture)];

    private static (int X, int Width)[] Bars()
    {
        int[] widths = [3, 1, 2, 1, 1, 3, 2, 1, 1, 2, 3, 1, 2, 2, 1, 1, 3, 1, 2, 1, 1, 2, 3, 1, 1, 2, 1, 3, 2, 1, 1, 2, 1, 1, 3, 2, 1, 2, 1, 3, 1, 1, 2, 2, 1, 3, 1, 2];
        var result = new List<(int, int)>();
        var x = 0;

        for (var i = 0; i < widths.Length; i++)
        {
            if (i % 2 is 0)
            {
                result.Add((x, widths[i] * 2));
            }

            x += widths[i] * 2;
        }

        return [.. result];
    }
}
