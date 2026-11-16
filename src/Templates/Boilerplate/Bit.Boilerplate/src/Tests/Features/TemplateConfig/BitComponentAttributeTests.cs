//+:cnd:noEmit
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Tests.Features.TemplateConfig;

[TestClass, TestCategory("UnitTest")]
public partial class BitComponentAttributeTests
{
    [TestMethod]
    public void EveryPascalCaseAttributeOfABitComponent_Should_BeOneOfItsParameters()
    {
        var root = BitComponentParameterTests.GetTemplateRoot();
        var razorFiles = BitComponentParameterTests.EnumerateRazorFiles(root).ToArray();
        var components = LoadBitComponents();

        Assert.IsGreaterThan(50, razorFiles.Length, "Almost no razor files were found, so this test would pass vacuously.");
        Assert.IsGreaterThan(100, components.Count, "Almost no Bit.BlazorUI components were found, so this test would pass vacuously.");

        var checkedAttributes = 0;
        var offenders = new List<string>();

        foreach (var file in razorFiles)
        {
            var text = File.ReadAllText(file);

            foreach (var (component, attribute, index) in EnumerateAttributes(text))
            {
                if (components.TryGetValue(component, out var componentType) is false) continue;

                checkedAttributes++;

                if (attribute is "Context" ||
                    BitComponentParameterTests.HasParameter(componentType, attribute) ||
                    (componentType.IsGenericTypeDefinition && componentType.GetGenericArguments().Any(a => a.Name == attribute))) continue;

                var lineNumber = text.Take(index).Count(c => c == '\n') + 1;

                offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber} -> <{component} {attribute} ...>");
            }
        }

        Assert.IsGreaterThan(1000, checkedAttributes, "Almost no attributes of Bit components were found, so this test would pass vacuously.");

        Assert.IsEmpty(offenders,
            $"A Bit component was passed an attribute that is not one of its parameters, usually a name Bit.BlazorUI has renamed. " +
            $"BitComponentBase does not throw for this - it routes the name into HtmlAttributes and splats it onto the root " +
            $"element, so the feature is silently off. Use the parameter's current name." +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static Dictionary<string, Type> LoadBitComponents()
    {
        _ = typeof(Bit.BlazorUI.BitButton);
        _ = typeof(Bit.BlazorUI.BitDataGrid<>);

        return AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(a => a.GetName().Name?.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is true)
                        .SelectMany(a => a.GetExportedTypes())
                        .Where(t => typeof(IComponent).IsAssignableFrom(t) && t.IsAbstract is false)
                        .GroupBy(t => t.Name.Split('`')[0])
                        .ToDictionary(g => g.Key, g => g.First());
    }

    [GeneratedRegex(@"<(Bit[A-Z]\w*)(?=[\s/>])")]
    private static partial Regex BitTagStart();

    private static IEnumerable<(string Component, string Attribute, int Index)> EnumerateAttributes(string text)
    {
        foreach (Match tag in BitTagStart().Matches(text))
        {
            var i = tag.Index + tag.Length;

            while (i < text.Length)
            {
                var c = text[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (c == '>' || (c == '/' && At(text, i + 1, '>'))) break;

                if (c == '@' && At(text, i + 1, '*'))
                {
                    var end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 2;
                    continue;
                }

                var start = i;
                while (i < text.Length && char.IsWhiteSpace(text[i]) is false && text[i] != '=' && text[i] != '>' && (text[i] == '/' && At(text, i + 1, '>')) is false) i++;
                var name = text[start..i];

                var j = i;
                while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
                if (At(text, j, '='))
                {
                    j++;
                    while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
                    i = SkipValue(text, j);
                }

                if (ParameterName(name) is { } parameter) yield return (tag.Groups[1].Value, parameter, start);
            }
        }
    }

    private static string? ParameterName(string attribute)
    {
        if (attribute.StartsWith("@bind-", StringComparison.Ordinal)) attribute = attribute["@bind-".Length..];
        else if (attribute.StartsWith('@')) return null;

        var colon = attribute.IndexOf(':');
        if (colon >= 0) attribute = attribute[..colon];

        return attribute.Length > 0 && char.IsUpper(attribute[0]) ? attribute : null;
    }

    private static bool At(string text, int index, char c) => index < text.Length && text[index] == c;

    private static int SkipValue(string text, int i)
    {
        if (i >= text.Length) return i;

        var quote = text[i];
        if (quote is '"' or '\'')
        {
            var k = i + 1;
            var code = false;
            var depth = 0;
            while (k < text.Length)
            {
                var c = text[k];
                if (c == quote && depth == 0) return k + 1;
                if (c == '@') code = true;
                if (code && depth > 0 && c is '"' or '\'') { k = SkipCSharpString(text, k); continue; }
                if (code && c is '(' or '[' or '{') depth++;
                else if (code && c is ')' or ']' or '}' && depth > 0) depth--;
                k++;
            }
            return k;
        }

        var n = i;
        var nesting = 0;
        while (n < text.Length)
        {
            var c = text[n];
            if (nesting == 0 && (char.IsWhiteSpace(c) || c == '>' || (c == '/' && At(text, n + 1, '>')))) return n;
            if (nesting > 0 && c is '"' or '\'') { n = SkipCSharpString(text, n); continue; }
            if (c is '(' or '[' or '{') nesting++;
            else if (c is ')' or ']' or '}' && nesting > 0) nesting--;
            n++;
        }
        return n;
    }

    private static int SkipCSharpString(string text, int i)
    {
        var quote = text[i];
        var interpolated = (i > 0 && text[i - 1] == '$') || (i > 1 && text[i - 2] == '$' && text[i - 1] == '@');
        var verbatim = (i > 0 && text[i - 1] == '@') || (i > 1 && text[i - 2] == '@' && text[i - 1] == '$');
        var k = i + 1;
        while (k < text.Length)
        {
            var c = text[k];
            if (c == '\\' && verbatim is false) { k += 2; continue; }
            if (c == quote)
            {
                if (verbatim && At(text, k + 1, quote)) { k += 2; continue; }
                return k + 1;
            }
            if (interpolated && c == '{')
            {
                if (At(text, k + 1, '{')) { k += 2; continue; }
                var depth = 1;
                k++;
                while (k < text.Length && depth > 0)
                {
                    var h = text[k];
                    if (h is '"' or '\'') { k = SkipCSharpString(text, k); continue; }
                    if (h == '{') depth++;
                    else if (h == '}') depth--;
                    k++;
                }
                continue;
            }
            k++;
        }
        return k;
    }
}
