using System.Globalization;

namespace Bit.BbScanner;

/// <summary>
/// One element of a GS1 element string: an Application Identifier and its data field, such as <c>(17)261231</c>, a
/// use by date of 31 December 2026.
/// </summary>
public sealed class BbGs1Element
{
    private readonly BbGs1AiDefinition _definition;

    internal BbGs1Element(BbGs1AiDefinition definition, string value)
    {
        _definition = definition;
        Value = value;
    }

    /// <summary>The Application Identifier, two to four digits, for example <c>01</c> or <c>3103</c>.</summary>
    public string ApplicationIdentifier => _definition.Ai;

    /// <summary>The data field exactly as the symbol carries it, without the AI or a separator.</summary>
    public string Value { get; }

    /// <summary>
    /// The GS1 data title of the AI, for example <c>GTIN</c>, <c>BATCH/LOT</c> or <c>NET WEIGHT (kg)</c>: a short
    /// label for showing the element, not a translation of it.
    /// </summary>
    public string Title => _definition.Title;

    /// <summary>
    /// How many of the value's last digits follow the implied decimal point, for the AIs that carry a measure, an
    /// amount, a price or a percentage - the last digit of the AI; <see langword="null"/> for every other AI.
    /// </summary>
    public int? DecimalPlaces => _definition.DecimalPlaces;

    /// <summary>
    /// The value as a number, with <see cref="DecimalPlaces"/> applied: <c>(3103)001250</c> is 1.250 kg and
    /// <c>(3922)1999</c> 19.99. For an amount or a price that names its currency - AIs 391n and 393n - the three digit
    /// ISO 4217 code is left out here and is in <see cref="CurrencyCode"/>. Also reads the counts of AIs 30 and 37.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns><see langword="false"/> when the AI does not carry a number.</returns>
    public bool TryGetDecimal(out decimal value)
    {
        value = 0;
        ReadOnlySpan<char> digits;
        switch (_definition.Kind)
        {
            case BbGs1ValueKind.Integer:
            case BbGs1ValueKind.Decimal:
                digits = Value;
                break;

            case BbGs1ValueKind.CurrencyDecimal:
                digits = Value.AsSpan(3);
                break;

            default:
                return false;
        }

        // At most fifteen digits: an exact decimal, with no rounding.
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var integer))
        {
            return false;
        }

        value = new decimal(integer) / Pow10(_definition.DecimalPlaces ?? 0);
        return true;
    }

    /// <summary>
    /// The ISO 4217 numeric currency code an amount or a price names - <c>978</c> for euros in <c>(3932)9781999</c>,
    /// for AIs 391n and 393n; <see langword="null"/> for every other AI.
    /// </summary>
    public string? CurrencyCode => _definition.Kind == BbGs1ValueKind.CurrencyDecimal ? Value[..3] : null;

    /// <summary>
    /// The date the value starts with, for the AIs that carry one: production, packaging, best before and expiry dates
    /// (11 to 17), delivery and release dates, first freeze, harvest, test by and production dates, and a date of birth.
    /// A day of <c>00</c> means the last day of the month. A two digit year is placed in the century that puts it
    /// closest to <paramref name="referenceYear"/>, per the GS1 General Specifications: up to 50 years ahead of it, or
    /// 49 behind.
    /// </summary>
    /// <param name="referenceYear">The year two digit years are read relative to, usually the current one.</param>
    /// <param name="date">The date; a time of day that follows it is not part of it.</param>
    /// <returns><see langword="false"/> when the AI does not carry a date.</returns>
    public bool TryGetDate(int referenceYear, out DateOnly date)
    {
        date = default;
        if (_definition.Kind != BbGs1ValueKind.Date || referenceYear is < 100 or > 9899)
        {
            return false;
        }

        var value = Value.AsSpan();
        var fourDigitYear = _definition.Components[0].Checks is ["yyyymmdd"] or ["yyyymmddhhmi"];
        int year;
        if (fourDigitYear)
        {
            year = int.Parse(value[..4], NumberStyles.None, CultureInfo.InvariantCulture);
            value = value[4..];
        }
        else
        {
            year = ResolveCentury(int.Parse(value[..2], NumberStyles.None, CultureInfo.InvariantCulture), referenceYear);
            value = value[2..];
        }

        var month = int.Parse(value[..2], NumberStyles.None, CultureInfo.InvariantCulture);
        var day = int.Parse(value[2..4], NumberStyles.None, CultureInfo.InvariantCulture);
        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            return false;
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (day == 0)
        {
            day = daysInMonth;
        }

        if (day > daysInMonth)
        {
            return false;
        }

        date = new DateOnly(year, month, day);
        return true;
    }

    /// <summary>
    /// <see cref="TryGetDate(int, out DateOnly)"/> with two digit years read relative to the current year, in UTC.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <returns><see langword="false"/> when the AI does not carry a date.</returns>
    public bool TryGetDate(out DateOnly date) => TryGetDate(DateTime.UtcNow.Year, out date);

    /// <summary>The element in the human readable form GS1 prints under a symbol: the AI in parentheses, then the value.</summary>
    public override string ToString() => $"({ApplicationIdentifier}){Value}";

    // GS1 General Specifications 7.12: a year 51 to 99 ahead of the reference one is last century's, and one 50 to
    // 99 behind it is next century's.
    private static int ResolveCentury(int twoDigitYear, int referenceYear)
    {
        var century = referenceYear / 100 * 100;
        var difference = twoDigitYear - (referenceYear % 100);
        if (difference >= 51)
        {
            century -= 100;
        }
        else if (difference <= -50)
        {
            century += 100;
        }

        return century + twoDigitYear;
    }

    private static decimal Pow10(int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
        {
            result *= 10;
        }

        return result;
    }
}
