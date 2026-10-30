// Page-label (/PageLabels) number-tree parsing.

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Builds the list of page labels from the catalog's <c>/PageLabels</c> number
/// tree. Each labelling range specifies a numbering style (decimal, roman,
/// alphabetic), an optional prefix, and an optional start value.
/// </summary>
internal static class BitPdfPageLabelBuilder
{
    private const long MaxLabelChars = 1L << 24;
    private const int MaxAlphaNumber = 26 * 64;
    private const int MaxRomanNumber = 100_000;

    public static IReadOnlyList<string> Build(IBitPdfXRef xref, BitPdfDict catalog, int pageCount)
    {
        var labels = new string[pageCount];

        // Default: the 1-based page number, used when no /PageLabels tree exists
        // and to fill any pages before the first labelling range.
        for (int i = 0; i < pageCount; i++)
        {
            labels[i] = (i + 1).ToString();
        }

        if (xref.FetchIfRef(catalog.Get("PageLabels")) is not BitPdfDict tree)
        {
            return labels;
        }

        // Collect (startPageIndex -> labelDict) pairs from the number tree.
        var ranges = new SortedDictionary<int, BitPdfDict>();
        CollectNums(xref, tree, ranges, new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);
        if (ranges.Count == 0)
        {
            return labels;
        }

        var keys = new List<int>(ranges.Keys);
        var budget = new BitPdfBudget(MaxLabelChars);
        for (int r = 0; r < keys.Count; r++)
        {
            int start = Math.Max(0, keys[r]);
            int end = r + 1 < keys.Count ? Math.Min(keys[r + 1], pageCount) : pageCount;
            BitPdfDict spec = ranges[keys[r]];

            string style = (spec.Get("S") as BitPdfName)?.Value ?? "";
            string prefix = (spec.Get("P") as BitPdfString)?.AsText() ?? "";
            int startAt = spec.Get("St") is double st ? (int)st : 1;

            for (int p = start; p < end; p++)
            {
                if (p < 0 || p >= pageCount)
                {
                    continue;
                }
                int value = startAt + (p - start);
                string number = Format(style, value);
                if (budget.TrySpend(prefix.Length + number.Length) is false)
                {
                    return labels;
                }
                labels[p] = prefix + number;
            }
        }

        return labels;
    }

    private static void CollectNums(IBitPdfXRef xref, BitPdfDict node, SortedDictionary<int, BitPdfDict> ranges, HashSet<object> visited, int depth)
    {
        if (depth > 32 || visited.Add(node) is false)
        {
            return;
        }

        if (xref.FetchIfRef(node.Get("Nums")) is List<object?> nums)
        {
            for (int i = 0; i + 1 < nums.Count; i += 2)
            {
                if (xref.FetchIfRef(nums[i]) is double key
                    && xref.FetchIfRef(nums[i + 1]) is BitPdfDict spec)
                {
                    ranges[(int)key] = spec;
                }
            }
        }

        if (xref.FetchIfRef(node.Get("Kids")) is List<object?> kids)
        {
            foreach (var kidObj in kids)
            {
                if (xref.FetchIfRef(kidObj) is BitPdfDict kid)
                {
                    CollectNums(xref, kid, ranges, visited, depth + 1);
                }
            }
        }
    }

    private static string Format(string style, int value) => style switch
    {
        "D" => value.ToString(),
        "r" => ToRoman(value).ToLowerInvariant(),
        "R" => ToRoman(value),
        "a" => ToAlpha(value, uppercase: false),
        "A" => ToAlpha(value, uppercase: true),
        // No style: the label is the prefix alone (empty number portion).
        _ => "",
    };

    private static string ToRoman(int number)
    {
        if (number is <= 0 or > MaxRomanNumber)
        {
            return number.ToString();
        }
        int[] values = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
        string[] symbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];
        var sb = new StringBuilder();
        int n = number;
        for (int i = 0; i < values.Length && n > 0; i++)
        {
            while (n >= values[i])
            {
                sb.Append(symbols[i]);
                n -= values[i];
            }
        }
        return sb.ToString();
    }

    // Spreadsheet-style labelling: 1->A, 26->Z, 27->AA, 28->BB (per the PDF spec,
    // each additional letter repeats: AA, BB, ... CCC).
    private static string ToAlpha(int number, bool uppercase)
    {
        if (number is <= 0 or > MaxAlphaNumber)
        {
            return number.ToString();
        }
        char baseChar = uppercase ? 'A' : 'a';
        int index = (number - 1) % 26;
        int repeat = (number - 1) / 26 + 1;
        return new string((char)(baseChar + index), repeat);
    }
}
