namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Which way the texts of a task read on the Progress page. An agent that writes Persian or Arabic keeps English names and
/// terms in it, so such a text often starts with a Latin word, which is all <c>dir="auto"</c> goes by, and its Latin letters
/// may well outnumber the others. So a task reads right to left when the language the agent gave it is written that way, or,
/// without one, once a quarter of its words are in a right-to-left script. In such a task, every text with a right-to-left
/// letter in it reads right to left and one without any, like a command, left to right; in a task that reads left to right,
/// a text reads right to left by the same quarter of its words.
/// </summary>
public static class WorkbenchTextDirection
{
    public const string RightToLeft = "rtl";
    public const string LeftToRight = "ltr";

    /// <summary>The direction of the task as a whole: its language's, or else that of the words of its title, summary and steps.</summary>
    public static string Of(WorkbenchProgressTask task)
    {
        if (IsRightToLeftLanguage(task.Language) is { } rightToLeft)
            return rightToLeft ? RightToLeft : LeftToRight;

        return ReadsRightToLeft([task.Title, task.Summary, .. task.Steps.Select(step => step.Text)]) ? RightToLeft : LeftToRight;
    }

    /// <summary>The direction of one text of a task that reads <paramref name="taskDirection"/>; a text without letters reads as the task.</summary>
    public static string Of(string? text, string taskDirection)
    {
        if (string.IsNullOrEmpty(text) || text.Any(char.IsLetter) is false)
            return taskDirection;

        if (taskDirection is RightToLeft)
            return text.Any(IsRightToLeftLetter) ? RightToLeft : LeftToRight;

        return ReadsRightToLeft([text]) ? RightToLeft : LeftToRight;
    }

    /// <summary>
    /// The direction as a bit BlazorUI component's Dir, which it needs given: left to itself it reads in the page's language,
    /// whatever the element around it says.
    /// </summary>
    public static BitDir ToBitDir(string direction) => direction is RightToLeft ? BitDir.Rtl : BitDir.Ltr;

    /// <summary>Whether a language tag, like fa or en-US, names a language written right to left; null for none.</summary>
    public static bool? IsRightToLeftLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(language.Trim()).TextInfo.IsRightToLeft;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>Whether at least a quarter of the words of the texts start with a right-to-left letter.</summary>
    private static bool ReadsRightToLeft(IEnumerable<string?> texts)
    {
        var words = 0;
        var rightToLeft = 0;

        foreach (var text in texts)
        {
            var inWord = false;

            foreach (var character in text ?? string.Empty)
            {
                if (char.IsLetter(character))
                {
                    if (inWord is false)
                    {
                        words++;
                        rightToLeft += IsRightToLeftLetter(character) ? 1 : 0;
                    }

                    inWord = true;
                }
                // A diacritic, like Persian's and Arabic's short vowels, is part of its word.
                else if (CharUnicodeInfo.GetUnicodeCategory(character) is not UnicodeCategory.NonSpacingMark)
                {
                    inWord = false;
                }
            }
        }

        return words > 0 && rightToLeft * 4 >= words;
    }

    /// <summary>A letter of Hebrew, Arabic, Persian, Urdu, Syriac, Thaana or another script written right to left.</summary>
    private static bool IsRightToLeftLetter(char character) => char.IsLetter(character) && character is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF';
}
