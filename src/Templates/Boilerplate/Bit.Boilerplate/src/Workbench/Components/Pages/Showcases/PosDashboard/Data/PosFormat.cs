namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>Formats the dashboard's money, counts and percentages in the current culture, with the euro as currency.</summary>
public static class PosFormat
{
    /// <summary>An amount in euros, in the culture's number format with the given decimals.</summary>
    public static string Money(decimal value, int decimals = 2)
    {
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.CurrencySymbol = "€";
        format.CurrencyDecimalDigits = decimals;

        return value.ToString("C", format);
    }

    /// <summary>Money short enough for the donut's center: thousands as k from ten thousand, cents only below a thousand.</summary>
    public static string CompactMoney(decimal value)
    {
        return value >= 10_000 ? $"{Money(Math.Round(value / 1000, 1), 1)}k" : Money(value, value >= 1_000 ? 0 : 2);
    }

    public static string Number(decimal value) => value.ToString("N0", CultureInfo.CurrentCulture);

    public static string Percent(decimal value) => value.ToString("P1", CultureInfo.CurrentCulture);

    /// <summary>A percentage with a plus sign for growth, as a KPI's change shows it.</summary>
    public static string SignedPercent(decimal value) => $"{(value >= 0 ? "+" : string.Empty)}{Percent(value)}";

    /// <summary>A bar's width as a percentage of the top value, written for CSS whatever the culture.</summary>
    public static string BarWidth(decimal value, decimal top) => FormattableString.Invariant($"{(top == 0 ? 0 : value / top * 100):0.#}%");
}
