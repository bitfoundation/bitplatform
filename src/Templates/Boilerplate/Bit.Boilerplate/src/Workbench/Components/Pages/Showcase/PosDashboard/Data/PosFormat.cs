namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

public static class PosFormat
{
    public static string Money(decimal value, int decimals = 2)
    {
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.CurrencySymbol = "€";
        format.CurrencyDecimalDigits = decimals;

        return value.ToString("C", format);
    }

    public static string CompactMoney(decimal value)
    {
        return value >= 10_000 ? $"{Money(Math.Round(value / 1000, 1), 1)}k" : Money(value, value >= 1_000 ? 0 : 2);
    }

    public static string Number(decimal value) => value.ToString("N0", CultureInfo.CurrentCulture);

    public static string Percent(decimal value) => value.ToString("P1", CultureInfo.CurrentCulture);

    public static string SignedPercent(decimal value) => $"{(value >= 0 ? "+" : string.Empty)}{Percent(value)}";
}
