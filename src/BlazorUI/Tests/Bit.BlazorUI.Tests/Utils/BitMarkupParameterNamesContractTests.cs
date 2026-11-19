using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins every capitalized attribute written on a library component in the markup of src/BlazorUI - the packages, the
/// demo app and the test hosts - to a parameter the component declares, in the same case.
/// </summary>
/// <remarks>
/// <para>
/// Nothing else catches one that is not: <c>BitComponentBase.SetParametersAsync</c> matches a name by ordinal comparison
/// and turns every name it does not know into an HTML attribute of the root, so a misspelled or renamed parameter
/// (<c>MaxChips</c> after the rename to <c>MaxDisplayedItems</c>, <c>Color</c> on a text field whose parameter is
/// <c>Accent</c>, <c>Autocomplete</c> for <c>AutoComplete</c>) compiles with no warning, renders as a dead attribute and
/// quietly stops the page or the test from covering the feature it was written for.
/// </para>
/// <para>
/// An HTML attribute meant for the root is written in lower case (<c>title</c>, <c>aria-label</c>, <c>data-*</c>), so
/// only the names that start with a capital letter are read. A directive (<c>@ref</c>, <c>@bind-Value</c>), a type
/// argument (<c>TItem</c>) and <c>Context</c> are no parameters and are skipped. The components are resolved by name
/// against the compiled packages; a tag naming no type of theirs (a test host's own component) is left alone.
/// </para>
/// </remarks>
[TestClass]
public class BitMarkupParameterNamesContractTests
{
    /// <summary>The assemblies whose components the markup is checked against.</summary>
    private static readonly string[] PackageAssemblies =
        ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.Assets", "Bit.BlazorUI.AI"];

    [TestMethod]
    public void EveryCapitalizedAttributeOfALibraryComponentShouldBeOneOfItsParameters()
    {
        var components = ReadComponents();

        Assert.IsGreaterThan(200, components.Count, $"Only {components.Count} components were found in the packages.");

        var files = Directory.EnumerateFiles(SourceFiles.Root, "*.razor", SearchOption.AllDirectories)
                             .Where(f => f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var parts &&
                                         parts.Contains("obj") is false && parts.Contains("bin") is false &&
                                         parts.Contains("node_modules") is false)
                             .Order(StringComparer.Ordinal)
                             .ToArray();

        var offenders = new List<string>();
        var checkedTags = 0;

        foreach (var file in files)
        {
            foreach (var tag in ReadTags(SourceFiles.ReadFullPath(file)))
            {
                if (components.TryGetValue(tag.Name, out var component) is false) continue;

                checkedTags++;

                foreach (var (attribute, line) in tag.Attributes)
                {
                    if (IsParameterCandidate(attribute) is false) continue;
                    if (component.Parameters.Contains(attribute) || component.TypeArguments.Contains(attribute)) continue;

                    var near = component.Parameters.FirstOrDefault(p => string.Equals(p, attribute, StringComparison.OrdinalIgnoreCase));

                    offenders.Add(near is null
                        ? $"{Relative(file)}:{line}: <{tag.Name}> has no parameter {attribute}."
                        : $"{Relative(file)}:{line}: <{tag.Name}> spells its parameter {near} as {attribute}.");
                }
            }
        }

        Assert.IsGreaterThan(5000, checkedTags, $"Only {checkedTags} tags of library components were read.");

        Assert.IsEmpty(offenders,
            "A capitalized attribute of a library component names no parameter of it, so BitComponentBase renders it as a " +
            "dead HTML attribute of the root. Use the parameter's name in its case, or write an HTML attribute in lower case:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void TheReadingShouldFollowTheRazorSyntax()
    {
        const string markup = """
            @* <BitButton Commented="x" /> *@
            <div title="<BitIcon Ignored />">
                <BitDropdown TItem="BitDropdownItem<string>" TValue="string"
                             DefaultValue="@("a > b")"
                             Items='_items' MultiSelect
                             OnChange="@(v => Handle(v, ")"))"
                             @bind-Value="_value" @ref="_dropdown"
                             data-x=@Data Label=Plain>
                    <BitText Typography="BitTypography.Caption1" />
                </BitDropdown>
            </div>
            <BitIcon Icon="@BitIconInfo.Bit("Bus")" Title="@L("id", Items[0]).Name" Body="@string.Join(" ", _lines)" />
            """;

        var tags = ReadTags(markup).ToArray();

        Assert.HasCount(3, tags);

        Assert.AreEqual("BitDropdown", tags[0].Name);
        CollectionAssert.AreEqual(
            new[] { "TItem", "TValue", "DefaultValue", "Items", "MultiSelect", "OnChange", "@bind-Value", "@ref", "data-x", "Label" },
            tags[0].Attributes.Select(a => a.Name).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 3, 4, 5, 5, 6, 7, 7, 8, 8 }, tags[0].Attributes.Select(a => a.Line).ToArray());

        Assert.AreEqual("BitText", tags[1].Name);
        CollectionAssert.AreEqual(new[] { "Typography" }, tags[1].Attributes.Select(a => a.Name).ToArray());

        Assert.AreEqual("BitIcon", tags[2].Name);
        CollectionAssert.AreEqual(new[] { "Icon", "Title", "Body" }, tags[2].Attributes.Select(a => a.Name).ToArray());

        Assert.IsTrue(IsParameterCandidate("Label"));
        Assert.IsFalse(IsParameterCandidate("@bind-Value"));
        Assert.IsFalse(IsParameterCandidate("title"));
        Assert.IsFalse(IsParameterCandidate("Context"));

        Assert.IsTrue(IsComponentName("BitButton"));
        Assert.IsTrue(IsComponentName("_BitDataGridRow"));
        Assert.IsFalse(IsComponentName("Bitter"));
        Assert.IsFalse(IsComponentName("_Item"));
    }

    private static bool IsParameterCandidate(string attribute) =>
        attribute.Length > 0 && char.IsUpper(attribute[0]) && attribute != "Context";

    // Every component of the packages by its name less the generic arity, with the names of its [Parameter]s, its own and
    // inherited, and of its type arguments. Two components of one name (none today) would have their names merged.
    private static Dictionary<string, (HashSet<string> Parameters, HashSet<string> TypeArguments)> ReadComponents()
    {
        var components = new Dictionary<string, (HashSet<string> Parameters, HashSet<string> TypeArguments)>(StringComparer.Ordinal);

        foreach (var assembly in PackageAssemblies.Select(Assembly.Load))
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.OfType<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || typeof(IComponent).IsAssignableFrom(type) is false) continue;

                var name = type.Name.Split('`')[0];

                if (components.TryGetValue(name, out var entry) is false)
                {
                    entry = (new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
                    components[name] = entry;
                }

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.IsDefined(typeof(ParameterAttribute), inherit: true)) entry.Parameters.Add(property.Name);
                }

                foreach (var argument in type.GetGenericArguments()) entry.TypeArguments.Add(argument.Name);
            }
        }

        return components;
    }

    // The opening tag of every element whose name starts with Bit and a capital letter, with the name and line of each
    // of its attributes. A Razor comment is skipped. Every other tag is read through too, so text inside one of its
    // values is never taken for a tag. A value is read as Razor reads it - quoted, or a bare token - with an @( ... ) or
    // an implicit expression (@L("id", Items), @string.Join(" ", ...)) inside it read to its end, past the strings, the
    // quotes and the > it may hold.
    private static IEnumerable<(string Name, List<(string Name, int Line)> Attributes)> ReadTags(string source)
    {
        var i = 0;
        var line = 1;

        while (i < source.Length)
        {
            if (source[i] == '\n')
            {
                line++;
                i++;
                continue;
            }

            if (At(source, i, "@*"))
            {
                var end = source.IndexOf("*@", i + 2, StringComparison.Ordinal);
                end = end < 0 ? source.Length : end + 2;
                line += Count(source, i, end);
                i = end;
                continue;
            }

            if (source[i] != '<' || i + 1 >= source.Length || char.IsLetter(source[i + 1]) is false)
            {
                i++;
                continue;
            }

            var nameStart = ++i;
            while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '-' or ':' or '.')) i++;
            var name = source[nameStart..i];

            var attributes = new List<(string Name, int Line)>();

            while (i < source.Length)
            {
                while (i < source.Length && char.IsWhiteSpace(source[i]))
                {
                    if (source[i] == '\n') line++;
                    i++;
                }

                if (i >= source.Length || source[i] == '>' || At(source, i, "/>")) break;

                var attributeStart = i;
                while (i < source.Length && char.IsWhiteSpace(source[i]) is false && source[i] is not ('=' or '>') && At(source, i, "/>") is false) i++;

                if (i == attributeStart)
                {
                    i++;
                    continue;
                }

                attributes.Add((source[attributeStart..i], line));

                if (i >= source.Length || source[i] != '=') continue;

                i++;
                var valueStart = i;
                i = SkipValue(source, i);
                line += Count(source, valueStart, i);
            }

            if (IsComponentName(name)) yield return (name, attributes);
        }
    }

    // A part a component renders for itself is spelled with a leading underscore (_BitNavBarChild, _BitDataGridRow).
    private static bool IsComponentName(string name) =>
        name.TrimStart('_') is var bare && bare.Length > 3 && bare.StartsWith("Bit", StringComparison.Ordinal) && char.IsUpper(bare[3]);

    private static int SkipValue(string source, int i)
    {
        if (i < source.Length && source[i] is '"' or '\'')
        {
            var quote = source[i++];

            while (i < source.Length && source[i] != quote)
            {
                i = source[i] == '@' ? SkipExpression(source, i + 1) : i + 1;
            }

            return Math.Min(i + 1, source.Length);
        }

        if (i < source.Length && source[i] == '@') return SkipExpression(source, i + 1);

        while (i < source.Length && char.IsWhiteSpace(source[i]) is false && source[i] != '>' && At(source, i, "/>") is false) i++;

        return i;
    }

    // The Razor expression after an @: an explicit @( ... ) to its matching parenthesis, or an implicit one - identifiers
    // joined by dots, each followed by any number of ( ... ) and [ ... ] groups.
    private static int SkipExpression(string source, int i)
    {
        if (i < source.Length && source[i] == '(') return SkipGroup(source, i);

        while (i < source.Length && (char.IsLetter(source[i]) || source[i] == '_'))
        {
            while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_')) i++;
            while (i < source.Length && source[i] is '(' or '[') i = SkipGroup(source, i);

            if (i + 1 < source.Length && source[i] == '.' && (char.IsLetter(source[i + 1]) || source[i + 1] == '_')) i++;
            else break;
        }

        return i;
    }

    // From an opening parenthesis or bracket to just past its match, past the C# strings and characters inside.
    private static int SkipGroup(string source, int i)
    {
        var depth = 0;

        while (i < source.Length)
        {
            var c = source[i];

            if (c is '"' or '\'')
            {
                i++;
                while (i < source.Length && source[i] != c) i += source[i] == '\\' ? 2 : 1;
            }
            else if (c is '(' or '[') depth++;
            else if (c is ')' or ']' && --depth == 0) return i + 1;

            i++;
        }

        return i;
    }

    private static bool At(string source, int i, string text) =>
        string.CompareOrdinal(source, i, text, 0, text.Length) == 0;

    private static int Count(string source, int start, int end)
    {
        var count = 0;
        for (var i = start; i < end && i < source.Length; i++) if (source[i] == '\n') count++;
        return count;
    }

    private static string Relative(string path) => Path.GetRelativePath(SourceFiles.Root, path).Replace('\\', '/');
}
