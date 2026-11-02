namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Reads a component's start tags in a .razor file as text: where each one is, and its attributes with where each sits,
/// stepping over Razor comments, quotes and the C# in @(...) so a '>' in them doesn't end the tag.
/// </summary>
public static class WorkbenchRazorTags
{
    /// <summary>The start of each tag of the component, in the order they appear, outside Razor and HTML comments.</summary>
    public static List<int> Find(string text, string name)
    {
        List<int> starts = [];

        for (var index = 0; index < text.Length; index++)
        {
            if (IsAt(text, index, "@*"))
            {
                index = End(text, index + 2, "*@");
                continue;
            }

            if (IsAt(text, index, "<!--"))
            {
                index = End(text, index + 4, "-->");
                continue;
            }

            // A '<' right after a name is C#, like List<BitButton>, not markup.
            if (text[index] is '<'
                && (index is 0 || char.IsLetterOrDigit(text[index - 1]) is false && text[index - 1] is not '_')
                && IsAt(text, index + 1, name)
                && index + 1 + name.Length < text.Length
                && (char.IsWhiteSpace(text[index + 1 + name.Length]) || text[index + 1 + name.Length] is '/' or '>'))
            {
                starts.Add(index);
            }
        }

        return starts;
    }

    /// <summary>The attributes of the tag that starts at the index, and where its name ends; null when the tag never closes.</summary>
    public static (List<WorkbenchRazorAttribute> Attributes, int NameEnd)? Parse(string text, int start, string name)
    {
        var nameEnd = start + 1 + name.Length;
        var index = nameEnd;
        List<WorkbenchRazorAttribute> attributes = [];

        while (true)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
                return null;

            if (text[index] is '>' || IsAt(text, index, "/>"))
                return (attributes, nameEnd);

            if (IsAt(text, index, "@*"))
            {
                index = End(text, index + 2, "*@") + 1;
                continue;
            }

            var attributeStart = index;

            while (index < text.Length && char.IsWhiteSpace(text[index]) is false && text[index] is not ('=' or '>') && IsAt(text, index, "/>") is false)
            {
                index++;
            }

            var attributeName = text[attributeStart..index];
            var afterName = index;

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length || text[index] is not '=')
            {
                attributes.Add(new(attributeName, null, attributeStart, afterName));
                index = afterName;
                continue;
            }

            index++;

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
                return null;

            string value;

            if (text[index] is '"' or '\'')
            {
                var quote = text[index];
                var valueStart = ++index;

                while (index < text.Length && text[index] != quote)
                {
                    index = text[index] is '@' ? SkipCode(text, index) : index + 1;
                }

                if (index >= text.Length)
                    return null;

                value = text[valueStart..index];
                index++;
            }
            else
            {
                var valueStart = index;

                if (text[index] is '@')
                {
                    index = SkipCode(text, index);
                }
                else
                {
                    while (index < text.Length && char.IsWhiteSpace(text[index]) is false && text[index] is not '>' && IsAt(text, index, "/>") is false)
                    {
                        index++;
                    }
                }

                value = text[valueStart..index];
            }

            attributes.Add(new(attributeName, value, attributeStart, index));
        }
    }

    /// <summary>From an @, the index after the code it starts: @@, @(...), or a name with its calls and indexers.</summary>
    private static int SkipCode(string text, int at)
    {
        var index = at + 1;

        if (index < text.Length && text[index] is '@')
            return index + 1;

        if (index < text.Length && text[index] is '(')
            return SkipBrackets(text, index);

        while (index < text.Length)
        {
            if (char.IsLetterOrDigit(text[index]) || text[index] is '_' or '.')
            {
                index++;
            }
            else if (text[index] is '(' or '[')
            {
                index = SkipBrackets(text, index);
            }
            else
            {
                break;
            }
        }

        return index;
    }

    /// <summary>From an opening bracket, the index after the one that closes it, stepping over C# strings and characters.</summary>
    private static int SkipBrackets(string text, int open)
    {
        var depth = 0;

        for (var index = open; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '"' or '\'':
                    var quote = text[index];
                    for (index++; index < text.Length && text[index] != quote; index++)
                    {
                        if (text[index] is '\\')
                        {
                            index++;
                        }
                    }
                    break;
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}':
                    if (--depth is 0)
                        return index + 1;
                    break;
            }
        }

        return text.Length;
    }

    private static bool IsAt(string text, int index, string value) => index + value.Length <= text.Length && string.CompareOrdinal(text, index, value, 0, value.Length) is 0;

    private static int End(string text, int from, string terminator)
    {
        var end = text.IndexOf(terminator, from, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + terminator.Length - 1;
    }
}
