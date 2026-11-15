using System.Diagnostics.CodeAnalysis;

namespace Bit.BbScanner;

/// <summary>Checks a data field against the format its AI defines.</summary>
internal static class BbGs1Validator
{
    // GS1 AI encodable character set 82: the printable ASCII characters but space, #, $, @, [, \, ], ^, `, {, |, }
    // and ~.
    private const string Cset82Punctuation = "!\"%&'()*+,-./:;<=>?_";

    internal static bool Validate(BbGs1AiDefinition definition, string value, [NotNullWhen(false)] out string? error)
    {
        var components = definition.Components;
        var position = 0;
        for (var i = 0; i < components.Length; i++)
        {
            var component = components[i];
            var remaining = value.Length - position;
            if (remaining == 0 && component.Optional)
            {
                break;
            }

            int length;
            if (component.IsFixed)
            {
                length = component.MaxLength;
                if (remaining < length)
                {
                    error = $"the value is too short; '{value}' needs {MinimumLength(components)} characters.";
                    return false;
                }
            }
            else
            {
                // A variable length component takes what the ones after it leave.
                length = Math.Min(remaining - FixedLengthAfter(components, i), component.MaxLength);
                if (length < component.MinLength)
                {
                    error = $"the value is too short; '{value}' needs {MinimumLength(components)} characters.";
                    return false;
                }
            }

            var part = value.AsSpan(position, length);
            if (!IsInCharset(part, component.Charset))
            {
                error = $"'{value}' holds characters its format does not allow.";
                return false;
            }

            foreach (var check in component.Checks)
            {
                if (!Check(check, part))
                {
                    error = $"'{value}' fails its {CheckName(check)} check.";
                    return false;
                }
            }

            position += length;
        }

        if (position != value.Length)
        {
            error = $"the value is too long; '{value}' allows {MaximumLength(components)} characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static int FixedLengthAfter(BbGs1Component[] components, int index)
    {
        var length = 0;
        for (var i = index + 1; i < components.Length; i++)
        {
            if (!components[i].Optional)
            {
                length += components[i].MinLength;
            }
        }

        return length;
    }

    private static int MinimumLength(BbGs1Component[] components)
    {
        var length = 0;
        foreach (var component in components)
        {
            if (!component.Optional)
            {
                length += component.MinLength;
            }
        }

        return length;
    }

    private static int MaximumLength(BbGs1Component[] components)
    {
        var length = 0;
        foreach (var component in components)
        {
            length += component.MaxLength;
        }

        return length;
    }

    private static bool IsInCharset(ReadOnlySpan<char> part, BbGs1Charset charset)
    {
        foreach (var character in part)
        {
            var allowed = charset switch
            {
                BbGs1Charset.Numeric => char.IsAsciiDigit(character),
                BbGs1Charset.Cset82 => char.IsAsciiLetterOrDigit(character) || Cset82Punctuation.Contains(character),
                BbGs1Charset.Cset39 => char.IsAsciiDigit(character) || char.IsAsciiLetterUpper(character) || character is '#' or '-' or '/',
                BbGs1Charset.Cset64 => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '=',
                _ => false,
            };

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static string CheckName(string check) => check switch
    {
        "csum" => "check digit",
        "csumalpha" => "check character pair",
        "iban" => "IBAN",
        "pcenc" => "percent-encoding",
        "latitude" or "longitude" => "coordinate range",
        "gcppos1" => "GS1 Company Prefix",
        "yymmd0" or "yymmdd" or "yyyymmdd" => "date",
        "yymmddhh" or "yymmddhhmi" or "yyyymmddhhmi" or "hhmi" or "mmoptss" => "date and time",
        _ => check,
    };

    private static bool Check(string check, ReadOnlySpan<char> part) => check switch
    {
        "csum" => IsValidCheckDigit(part),
        "yymmd0" => IsDate(part, fourDigitYear: false, dayZeroAllowed: true),
        "yymmdd" => IsDate(part, fourDigitYear: false, dayZeroAllowed: false),
        "yyyymmdd" => IsDate(part, fourDigitYear: true, dayZeroAllowed: false),
        "yymmddhh" => IsDate(part[..6], fourDigitYear: false, dayZeroAllowed: false) && IsInRange(part[6..8], 0, 23),
        "yymmddhhmi" => IsDate(part[..6], fourDigitYear: false, dayZeroAllowed: false) && IsTime(part[6..10]),
        "yyyymmddhhmi" => IsDate(part[..8], fourDigitYear: true, dayZeroAllowed: false) && IsTime(part[8..12]),
        "hhmi" => IsTime(part),
        // Minutes, and optionally seconds after them.
        "mmoptss" => part.Length is 2 or 4 && IsInRange(part[..2], 0, 59) && (part.Length == 2 || IsInRange(part[2..4], 0, 59)),
        "yesno" => part is "0" or "1",
        // ISO/IEC 5218: not known, male, female, not applicable.
        "iso5218" => part is "0" or "1" or "2" or "9",
        "nozeroprefix" => part.Length == 1 || part[0] != '0',
        "zero" => part is "0",
        "nonzero" => part.ContainsAnyExcept('0'),
        // A key that starts with a GS1 Company Prefix starts with at least two digits.
        "gcppos1" => part.Length >= 2 && char.IsAsciiDigit(part[0]) && char.IsAsciiDigit(part[1]),
        // Winding direction: face out, face in, undefined.
        "winding" => part is "0" or "1" or "9",
        "csumalpha" => IsValidCheckCharacterPair(part),
        "iban" => IsValidIban(part),
        "pcenc" => IsValidPercentEncoding(part),
        // Degrees plus 90 and 180, in ten millionths: latitude up to 180, longitude up to 360.
        "latitude" => Number64(part) <= 1_800_000_000,
        "longitude" => Number64(part) <= 3_600_000_000,
        "hyphen" => part is "-",
        "slash" => part is "/",
        // A birth sequence: the position, a slash and the number of births, the position from 1 up to that number.
        "posinseqslash" => part.Length == 3 && part[1] == '/' && part[0] is >= '1' and <= '9' && part[2] is >= '1' and <= '9' && part[0] <= part[2],
        "hasnondigit" => part.ContainsAnyExcept("0123456789"),
        "iso3166alpha2" => char.IsAsciiLetterUpper(part[0]) && char.IsAsciiLetterUpper(part[1]),
        "iso3166list" => part.Length % 3 == 0,
        // A piece number and the total: 01 to 99, the piece no more than the total.
        "pieceoftotal" => IsInRange(part[..2], 1, 99) && IsInRange(part[2..4], 1, 99) && Number(part[..2]) <= Number(part[2..4]),
        // Numeric country and currency codes, checked to be digits only.
        "iso3166" or "iso4217" => true,
        _ => throw new InvalidOperationException($"Unknown GS1 check '{check}'."),
    };

    /// <summary>The GS1 modulo 10 check digit: weights 3 and 1 from the right, the rightmost being the check digit.</summary>
    internal static bool IsValidCheckDigit(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (var i = digits.Length - 2; i >= 0; i--)
        {
            var weight = ((digits.Length - 2 - i) & 1) == 0 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        return (10 - (sum % 10)) % 10 == digits[^1] - '0';
    }

    // GS1 General Specifications 7.9.5: the alphanumeric check character pair of a GMN or MUDI. Each character but the
    // last two is weighted, right to left, by the primes from 2 up; the sum modulo 1021 is written as two characters of
    // character set 32, its high five bits first.
    private const string Cset82 = "!\"%&'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
    private const string Cset32 = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly int[] Primes = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83];

    internal static bool IsValidCheckCharacterPair(ReadOnlySpan<char> value)
    {
        var data = value.Length - 2;
        if (data < 0 || data > Primes.Length)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < data; i++)
        {
            sum += Cset82.IndexOf(value[i], StringComparison.Ordinal) * Primes[data - 1 - i];
        }

        sum %= 1021;
        return value[data] == Cset32[sum >> 5] && value[data + 1] == Cset32[sum & 31];
    }

    // ISO 13616: two capitals for the country, two check digits, and up to thirty letters and digits; moved to the
    // end and read with letters as 10 to 35, the whole is 1 modulo 97.
    internal static bool IsValidIban(ReadOnlySpan<char> value)
    {
        if (value.Length < 5 ||
            !char.IsAsciiLetterUpper(value[0]) || !char.IsAsciiLetterUpper(value[1]) ||
            !char.IsAsciiDigit(value[2]) || !char.IsAsciiDigit(value[3]))
        {
            return false;
        }

        var remainder = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[(i + 4) % value.Length];
            if (char.IsAsciiDigit(character))
            {
                remainder = ((remainder * 10) + (character - '0')) % 97;
            }
            else if (char.IsAsciiLetterUpper(character))
            {
                remainder = ((remainder * 100) + (character - 'A' + 10)) % 97;
            }
            else
            {
                return false;
            }
        }

        return remainder == 1;
    }

    // A percent sign starts an escape of exactly two hexadecimal digits.
    private static bool IsValidPercentEncoding(ReadOnlySpan<char> value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '%' && (i + 2 >= value.Length || !char.IsAsciiHexDigit(value[i + 1]) || !char.IsAsciiHexDigit(value[i + 2])))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDate(ReadOnlySpan<char> part, bool fourDigitYear, bool dayZeroAllowed)
    {
        var yearLength = fourDigitYear ? 4 : 2;
        var year = Number(part[..yearLength]);
        var month = Number(part.Slice(yearLength, 2));
        var day = Number(part.Slice(yearLength + 2, 2));
        if (month is < 1 or > 12 || (fourDigitYear && year == 0))
        {
            return false;
        }

        if (day == 0)
        {
            return dayZeroAllowed;
        }

        // A two digit year's century is only known relative to the date it is read on; within the hundred years
        // GS1 places it in, only 2100 is a multiple of four that is not a leap year, so 29 February is accepted
        // whenever the year is a multiple of four.
        var leap = fourDigitYear ? DateTime.IsLeapYear(Math.Max(year, 1)) : year % 4 == 0;
        var daysInMonth = month == 2 ? (leap ? 29 : 28) : DateTime.DaysInMonth(2001, month);
        return day <= daysInMonth;
    }

    private static bool IsTime(ReadOnlySpan<char> part) => IsInRange(part[..2], 0, 23) && IsInRange(part[2..4], 0, 59);

    private static bool IsInRange(ReadOnlySpan<char> digits, int min, int max)
    {
        var value = Number(digits);
        return value >= min && value <= max;
    }

    private static long Number64(ReadOnlySpan<char> digits)
    {
        var value = 0L;
        foreach (var digit in digits)
        {
            value = (value * 10) + (digit - '0');
        }

        return value;
    }

    private static int Number(ReadOnlySpan<char> digits)
    {
        var value = 0;
        foreach (var digit in digits)
        {
            value = (value * 10) + (digit - '0');
        }

        return value;
    }
}
