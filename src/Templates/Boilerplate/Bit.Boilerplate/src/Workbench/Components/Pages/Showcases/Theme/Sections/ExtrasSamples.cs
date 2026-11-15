namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The sample data of the Extras section: a coffee shop's orders, products, stores and sales figures. All of it is fixed,
/// and the dates are built around <see cref="ThemeSamples.Today"/>, so every run and every screenshot shows the same.
/// </summary>
public static class ExtrasSamples
{
    /// <summary>The number of the latest order; the long order lists count down from it.</summary>
    public const int LatestOrderNumber = 8042;

    /// <summary>The latest orders, for the data grid. The first one is the invoice the PDF viewer shows.</summary>
    public static readonly ExtrasOrder[] Orders =
    [
        new("8042", "Emma de Vries", ThemeSamples.Today.AddMinutes(-25), 12.50m, ExtrasOrderStatus.Paid),
        new("8041", "Lucas Jansen", ThemeSamples.Today.AddMinutes(-70), 31.80m, ExtrasOrderStatus.Pending),
        new("8040", "Mila Bakker", ThemeSamples.Today.AddHours(-3), 8.40m, ExtrasOrderStatus.Paid),
        new("8039", "Sem Visser", ThemeSamples.Today.AddDays(-1).AddHours(2), 54.20m, ExtrasOrderStatus.Refunded),
        new("8038", "Julia Smit", ThemeSamples.Today.AddDays(-1).AddHours(-1), 19.90m, ExtrasOrderStatus.Paid),
        new("8037", "Noah Meijer", ThemeSamples.Today.AddDays(-2), 26.75m, ExtrasOrderStatus.Pending)
    ];

    /// <summary>The products the long order lists draw from, as resource keys with their prices.</summary>
    public static readonly (string NameKey, decimal Price)[] Products =
    [
        (nameof(WorkbenchStrings.PosProductFlatWhite), 3.50m),
        (nameof(WorkbenchStrings.PosProductOatLatte), 4.20m),
        (nameof(WorkbenchStrings.PosProductCroissant), 3.25m),
        (nameof(WorkbenchStrings.PosProductAppleTart), 3.95m),
        (nameof(WorkbenchStrings.PosProductCappuccino), 3.40m),
        (nameof(WorkbenchStrings.PosProductStroopwafel), 2.25m),
        (nameof(WorkbenchStrings.PosProductSourdough), 6.50m),
        (nameof(WorkbenchStrings.PosProductColdBrew), 4.00m)
    ];

    /// <summary>The stores the map marks, as resource keys with where they are.</summary>
    public static readonly (string NameKey, double Latitude, double Longitude)[] Stores =
    [
        (nameof(WorkbenchStrings.PosStoreAmsterdam), 52.3744, 4.8810),
        (nameof(WorkbenchStrings.PosStoreRotterdam), 51.9200, 4.4865),
        (nameof(WorkbenchStrings.PosStoreUtrecht), 52.0908, 5.1215)
    ];

    /// <summary>
    /// The staff of the resource timeline, as resource keys with the store they work at and a week of shifts, Monday first:
    /// O opening, M midday, B bakery, C closing, - a day off.
    /// </summary>
    public static readonly (string NameKey, string StoreKey, string Week)[] Staff =
    [
        (nameof(WorkbenchStrings.PosStaffEmma), nameof(WorkbenchStrings.PosStoreAmsterdam), "OOC-CO-"),
        (nameof(WorkbenchStrings.PosStaffLucas), nameof(WorkbenchStrings.PosStoreAmsterdam), "C-OOC-O"),
        (nameof(WorkbenchStrings.PosStaffMila), nameof(WorkbenchStrings.PosStoreAmsterdam), "BB-BBB-"),
        (nameof(WorkbenchStrings.PosStaffSem), nameof(WorkbenchStrings.PosStoreRotterdam), "MCC-CC-"),
        (nameof(WorkbenchStrings.PosStaffNoah), nameof(WorkbenchStrings.PosStoreRotterdam), "OM-MOM-"),
        (nameof(WorkbenchStrings.PosStaffJulia), nameof(WorkbenchStrings.PosStoreUtrecht), "OM-MO-M")
    ];

    /// <summary>The hours a shift letter of <see cref="Staff"/> starts and ends at, with the resource key of its name.</summary>
    public static (int Start, int End, string NameKey) Shift(char letter) => letter switch
    {
        'O' => (7, 15, nameof(WorkbenchStrings.ShiftsOpening)),
        'M' => (10, 18, nameof(WorkbenchStrings.ShiftsMidday)),
        'B' => (7, 13, nameof(WorkbenchStrings.ShiftsBakery)),
        _ => (11, 19, nameof(WorkbenchStrings.ShiftsClosing))
    };

    /// <summary>Revenue of the last six months, the latest last, in euros.</summary>
    public static readonly double[] RevenueThisYear = [18200, 16900, 21400, 23800, 22100, 26500];

    /// <summary>Revenue of the same six months a year earlier, in euros.</summary>
    public static readonly double[] RevenueLastYear = [15400, 15100, 18800, 19600, 20900, 21700];

    /// <summary>Visitors per day this week, Monday first.</summary>
    public static readonly double[] VisitorsThisWeek = [420, 465, 510, 488, 612, 734, 690];

    /// <summary>Visitors per day last week, Monday first.</summary>
    public static readonly double[] VisitorsLastWeek = [390, 410, 455, 470, 540, 655, 640];

    /// <summary>This month's sales per payment method, as resource keys with the amount in euros.</summary>
    public static readonly (string MethodKey, double Amount)[] Payments =
    [
        (nameof(WorkbenchStrings.PosPaymentCard), 5240),
        (nameof(WorkbenchStrings.PosPaymentWallet), 3120),
        (nameof(WorkbenchStrings.PosPaymentCash), 1460),
        (nameof(WorkbenchStrings.PosPaymentGiftCard), 380)
    ];

    /// <summary>An amount in euros, in the current culture's number format.</summary>
    public static string Money(decimal value)
    {
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.CurrencySymbol = "€";
        format.CurrencyDecimalDigits = 2;

        return value.ToString("C", format);
    }
}
