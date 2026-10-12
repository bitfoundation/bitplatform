using System.Text;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The list of countries a country selector offers and the search over it, shared by every component that
/// lists the countries of <see cref="BitCountries"/> (<see cref="BitPhoneInput"/>, <see cref="BitCountryPicker"/>),
/// so the same term finds the same countries in the same order wherever it is typed.
/// </summary>
internal static class BitCountrySearch
{
    internal const int DialCodeExactRank = 3;
    internal const int DialCodeStartsRank = 4;
    internal const int DialCodeContainsRank = 7;



    // The countries in the order a selector shows them: the preferred ones first, in the order they were given,
    // then the rest in the order of the countries list, less the excluded ones. How many of them are there because
    // they were preferred is handed back as well, which is where the unfiltered list draws the rule that sets them
    // apart from the rest.
    internal static List<BitCountry> BuildItems(ICollection<BitCountry> countries,
                                                ICollection<BitCountry>? excludeCountries,
                                                ICollection<BitCountry>? preferredCountries,
                                                out int preferredCount)
    {
        preferredCount = 0;

        var excluded = excludeCountries is not null && excludeCountries.Count > 0
                            ? new HashSet<string>(excludeCountries.Select(c => c.Iso2), StringComparer.OrdinalIgnoreCase)
                            : null;

        // A country is offered once, by the first entry of the list that carries its ISO code: the code is what
        // tells a row apart from the others (its key, its selected state), and a list that names a country twice -
        // the same one repeated, or a translated entry beside the built-in one - would otherwise render two rows
        // nothing could tell apart. It is also a copy even when the source already is a List: the folded names are
        // read by the index of the list, and a list the caller keeps a reference to could otherwise grow underneath
        // them without the parameter reference ever changing.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<BitCountry> source = [.. countries.Where(c => (excluded is null || excluded.Contains(c.Iso2) is false) && seen.Add(c.Iso2))];

        if (preferredCountries is null || preferredCountries.Count == 0) return source;

        var preferred = new List<BitCountry>();
        var preferredIso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var country in preferredCountries)
        {
            // Only the countries the selector actually offers can be pinned to the top of it.
            var match = source.Find(c => c.Iso2.Equals(country.Iso2, StringComparison.OrdinalIgnoreCase));
            if (match is null || preferredIso.Add(match.Iso2) is false) continue;

            preferred.Add(match);
        }

        preferredCount = preferred.Count;

        return [.. preferred, .. source.Where(c => preferredIso.Contains(c.Iso2) is false)];
    }

    // The words of a folded name, which a term of several words is matched against word by word.
    internal static string[] SplitWords(string foldedName) => foldedName.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);

    // The ranks of a match found through the dialing code rather than through a name or an ISO code.
    internal static bool IsDialCodeRank(int rank) => rank is DialCodeExactRank or DialCodeStartsRank or DialCodeContainsRank;

    // The rank of a country against a search term, lower being closer. A negative rank means the country does not
    // match the term at all. An empty code term leaves the dialing codes out of the search, which is how a selector
    // that never shows them asks for a search by name and ISO code alone.
    internal static int GetSearchRank(BitCountry country, string name, string[] words, string text, string codeTerm)
    {
        var codes = codeTerm.Length > 0 ? country.DigitsCodes : [];

        if (country.Iso2.Equals(text, StringComparison.OrdinalIgnoreCase)) return 0;
        if (country.Iso3.Equals(text, StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) return 2;
        if (AnyCode(codes, codeTerm, static (code, term) => code.Equals(term, StringComparison.Ordinal))) return DialCodeExactRank;
        if (AnyCode(codes, codeTerm, static (code, term) => code.StartsWith(term, StringComparison.Ordinal))) return DialCodeStartsRank;

        // A term of several words matches a name whose words start with them, so "united sta" and
        // "so afr" both find their country without the term having to be a prefix of the whole name.
        if (MatchesWordStarts(words, text)) return 5;

        if (name.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 6;
        if (AnyCode(codes, codeTerm, static (code, term) => code.Contains(term, StringComparison.Ordinal))) return DialCodeContainsRank;
        if (country.Iso2.Contains(text, StringComparison.OrdinalIgnoreCase)) return 8;
        if (country.Iso3.Contains(text, StringComparison.OrdinalIgnoreCase)) return 9;

        return -1;
    }

    // The index of the first country from the start index on (wrapping around the end of the list) whose name starts
    // with the folded term, or -1 where none does. The folded names are the ones folded once with the list, when they
    // belong to the very list walked over, and are folded here otherwise.
    internal static int FindByNameStart(IReadOnlyList<BitCountry> items, string[]? foldedNames, string term, int start)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var index = (start + i) % items.Count;
            if (index < 0) index += items.Count;

            var name = foldedNames is not null ? foldedNames[index] : Fold(items[index].Name);

            if (name.StartsWith(term, StringComparison.InvariantCultureIgnoreCase)) return index;
        }

        return -1;
    }

    // A copy of the text with the diacritic of each character removed, so a country is found by the
    // plain letters of its name ("Aland", "Reunion", "Curacao") as much as by the accented ones.
    internal static string Fold(string text)
    {
        // Most country names are plain ASCII, which has no diacritic to fold: they are returned as
        // they are instead of being copied character by character into a builder.
        if (Ascii.IsValid(text)) return text;

        var builder = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            // ASCII has no diacritics to fold, and a lone surrogate half (one of the two chars an
            // emoji is made of) is not a valid string of its own, so normalizing it would throw.
            if (char.IsAscii(c) || char.IsSurrogate(c))
            {
                builder.Append(c);
                continue;
            }

            var baseChar = c;

            foreach (var decomposed in c.ToString().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(decomposed) == UnicodeCategory.NonSpacingMark) continue;

                baseChar = decomposed;
                break;
            }

            builder.Append(baseChar);
        }

        return builder.ToString();
    }



    // A country is found by any of the codes it answers to, so a term is measured against all of them.
    private static bool AnyCode(string[] codes, string term, Func<string, string, bool> matches)
    {
        if (term.Length == 0) return false;

        foreach (var code in codes)
        {
            if (matches(code, term)) return true;
        }

        return false;
    }

    private static bool MatchesWordStarts(string[] words, string text)
    {
        if (text.Contains(' ') is false) return false;

        var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length == 0 || terms.Length > words.Length) return false;

        for (var i = 0; i < terms.Length; i++)
        {
            if (words[i].StartsWith(terms[i], StringComparison.InvariantCultureIgnoreCase) is false) return false;
        }

        return true;
    }
}
