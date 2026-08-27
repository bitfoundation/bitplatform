//-:cnd:noEmit
// Conditional processing is off for this whole file, and the marker above has to stay on the very first line.
// The doc comments below quote the template's own XML-comment conditional syntax; with processing on, the engine
// would read those quotes as real directives.

using System.Xml.Linq;

namespace Boilerplate.Tests.Features.Culture;

/// <summary>
/// The ten locales of each resource family are not written by hand. <c>Bit.ResxTranslator</c> fills the nine
/// translations from the neutral file with an LLM, and the four CD pipelines run it on every deploy. That is the
/// whole reason these assertions exist: the failure mode of a machine translator is not a compile error, it is a
/// value that is subtly wrong in exactly one language, in a resource nobody on the team reads.
/// <para>
/// Four invariants, each of which has already been violated at least once:
/// </para>
/// <list type="number">
/// <item><b>Same keys everywhere.</b> A key missing from one locale falls back to the neutral file, so a single
/// English sentence appears in the middle of a translated screen and nothing anywhere reports it.</item>
/// <item><b>Same placeholders everywhere.</b> <c>AppStrings.hi.resx</c> once gave <c>YouAreSignedInAs</c> a
/// <c>{0}</c> that the neutral value does not have and that the only call site never fills, so Hindi users read a
/// literal brace. The mirror failure - a translation that <i>drops</i> a placeholder the call site does pass -
/// silently swallows the value instead.</item>
/// <item><b>No key declared twice in one file.</b> <c>AiChatPanelPrompt3</c> was once three sibling
/// <c>&lt;data&gt;</c> elements, one per <c>module</c> branch. Because the branch markers are XML comments, all
/// three were live XML in this tree: ResGen emitted twenty <c>MSB3568</c> warnings and silently kept the first.
/// The fix keeps one <c>&lt;data&gt;</c> and moves the branches down to its <c>&lt;value&gt;</c> children, which is
/// exactly the shape assertion 3 pins.</item>
/// <item><b>No markup in any value.</b> Nothing renders these strings through <c>MarkupString</c> today, and this
/// keeps it that way from the other end: a translated value that grows an <c>&lt;a&gt;</c> or an
/// <c>&lt;img onerror&gt;</c> is a supply-chain injection waiting for the first call site that does.</item>
/// </list>
/// <para>
/// Runs against the template's own working copy. A generated project has pruned its resources to one configuration
/// and has no <c>.template.config</c>, so there this suite reports inconclusive rather than failing.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ResxParityTests
{
    /// <summary>
    /// A composite-format placeholder: <c>{0}</c>, <c>{1}</c>, and the alignment/format-specifier forms
    /// <c>{0,-10}</c> / <c>{0:N2}</c>. Only the index matters for parity - a locale is free to reorder.
    /// </summary>
    private static readonly Regex placeholderRegex = new(@"\{(\d+)(?:,-?\d+)?(?::[^}]*)?\}", RegexOptions.Compiled);

    /// <summary>Any HTML/XML element tag, opening or closing.</summary>
    private static readonly Regex markupRegex = new(@"</?[a-zA-Z][a-zA-Z0-9]*\b[^>]*>", RegexOptions.Compiled);

    [TestMethod]
    public void EveryLocale_Should_DeclareTheSameKeysAsItsNeutralFile()
    {
        var failures = new List<string>();

        foreach (var family in ResourceFamilies())
        {
            var neutralKeys = KeysOf(family.NeutralFile);

            Assert.IsGreaterThan(0, neutralKeys.Count,
                $"{Path.GetFileName(family.NeutralFile)} parsed to zero keys - the parser, not the resource, is broken.");

            foreach (var locale in family.LocaleFiles)
            {
                var localeKeys = KeysOf(locale);
                var missing = neutralKeys.Except(localeKeys).Order().ToArray();
                var extra = localeKeys.Except(neutralKeys).Order().ToArray();

                if (missing.Length > 0)
                    failures.Add($"{Path.GetFileName(locale)} is missing {missing.Length} key(s): {string.Join(", ", missing)}");

                if (extra.Length > 0)
                    failures.Add($"{Path.GetFileName(locale)} declares {extra.Length} key(s) the neutral file does not: {string.Join(", ", extra)}");
            }
        }

        Assert.IsEmpty(failures,
            $"Locale files have drifted from their neutral file:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public void EveryLocale_Should_UseTheSamePlaceholdersAsItsNeutralFile()
    {
        var failures = new List<string>();

        foreach (var family in ResourceFamilies())
        {
            var neutral = ValuesOf(family.NeutralFile);

            foreach (var locale in family.LocaleFiles)
            {
                foreach (var (key, localeValues) in ValuesOf(locale))
                {
                    if (neutral.TryGetValue(key, out var neutralValues) is false)
                        continue; // reported by the key-parity test instead.

                    // A key can legitimately carry several values - one per template branch. Compare them in order.
                    for (var i = 0; i < Math.Min(neutralValues.Count, localeValues.Count); i++)
                    {
                        var expected = PlaceholderIndexes(neutralValues[i]);
                        var actual = PlaceholderIndexes(localeValues[i]);

                        if (expected.SetEquals(actual) is false)
                        {
                            failures.Add(
                                $"{Path.GetFileName(locale)} :: {key} uses {{{string.Join(",", actual.Order())}}} " +
                                $"where the neutral value uses {{{string.Join(",", expected.Order())}}} -> \"{localeValues[i]}\"");
                        }
                    }
                }
            }
        }

        Assert.IsEmpty(failures,
            $"A translated value's placeholders no longer match the neutral value's. Either the call site never fills " +
            $"the extra one (it renders as a literal brace) or it fills one that has been dropped (the value is " +
            $"silently swallowed):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public void NoResourceFile_Should_DeclareTheSameKeyTwice()
    {
        var failures = new List<string>();

        foreach (var file in ResourceFamilies().SelectMany(f => f.AllFiles))
        {
            var duplicates = DataElements(file).GroupBy(NameOf)
                                               .Where(g => g.Count() > 1)
                                               .Select(g => $"{g.Key} (x{g.Count()})")
                                               .ToArray();

            if (duplicates.Length > 0)
                failures.Add($"{Path.GetFileName(file)}: {string.Join(", ", duplicates)}");
        }

        Assert.IsEmpty(failures,
            $"A .resx declares the same key more than once. ResGen answers this with MSB3568 and keeps the FIRST " +
            $"occurrence, so the others are silently discarded. If the intent was one value per template branch, put " +
            $"the branch markers inside a single <data> element, around its <value> children:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public void NoResourceValue_Should_BeEmptyOrCarryMarkup()
    {
        var failures = new List<string>();

        foreach (var file in ResourceFamilies().SelectMany(f => f.AllFiles))
        {
            foreach (var (key, values) in ValuesOf(file))
            {
                foreach (var value in values)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        failures.Add($"{Path.GetFileName(file)} :: {key} is empty");

                    var markup = markupRegex.Match(value);
                    if (markup.Success)
                        failures.Add($"{Path.GetFileName(file)} :: {key} contains markup \"{markup.Value}\" -> \"{value}\"");
                }
            }
        }

        Assert.IsEmpty(failures,
            $"An empty value renders as nothing at all; a value carrying markup is one MarkupString call site away " +
            $"from being an injection vector, and these values are machine-generated:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private record ResourceFamily(string NeutralFile, IReadOnlyList<string> LocaleFiles)
    {
        public IEnumerable<string> AllFiles => LocaleFiles.Prepend(NeutralFile);
    }

    /// <summary>
    /// The three families, resolved from disk rather than listed, so a new locale is covered the day it is added.
    /// <c>EmailStrings</c> is globbed through <c>src/Server/*</c> because the project directory carries the
    /// generated project's name in every configuration but this one.
    /// </summary>
    private static IEnumerable<ResourceFamily> ResourceFamilies()
    {
        var templateRoot = FindTemplateRoot();

        string[] neutralFiles =
        [
            Path.Combine(templateRoot, "src", "Shared", "Resources", "AppStrings.resx"),
            Path.Combine(templateRoot, "src", "Shared", "Resources", "IdentityStrings.resx"),
            .. Directory.GetFiles(Path.Combine(templateRoot, "src", "Server"), "EmailStrings.resx", SearchOption.AllDirectories)
        ];

        foreach (var neutral in neutralFiles)
        {
            var directory = Path.GetDirectoryName(neutral)!;
            var stem = Path.GetFileNameWithoutExtension(neutral);

            // AppStrings.fa.resx yes, AppStrings.resx no - the neutral file is the baseline, not a locale.
            var locales = Directory.GetFiles(directory, $"{stem}.*.resx")
                                   .Where(f => string.Equals(f, neutral, StringComparison.OrdinalIgnoreCase) is false)
                                   .Order()
                                   .ToArray();

            Assert.IsGreaterThan(0, locales.Length, $"Found no locale files next to {neutral}.");

            yield return new ResourceFamily(neutral, locales);
        }
    }

    /// <summary>
    /// The real entries only. Every .resx opens with the ResX schema documentation comment, which contains four
    /// example entries - <c>Name1</c>, <c>Color1</c>, <c>Bitmap1</c>, <c>Icon1</c>. They are inside an XML comment,
    /// so an XML reader skips them and a naive text scan does not; that difference is worth four phantom "unused
    /// key" reports per file to anyone who scripts this with a regex.
    /// </summary>
    private static IEnumerable<XElement> DataElements(string file) =>
        XDocument.Load(file).Root!.Elements("data")
                                  .Where(d => d.Attribute("mimetype") is null && d.Attribute("type") is null);

    private static string NameOf(XElement data) => data.Attribute("name")!.Value;

    private static HashSet<string> KeysOf(string file) => DataElements(file).Select(NameOf).ToHashSet();

    /// <summary>
    /// Key to its values, in document order. Normally one value per key; a key whose value is selected by a
    /// template conditional carries one per branch, and those are compared branch-for-branch across locales.
    /// </summary>
    private static Dictionary<string, IReadOnlyList<string>> ValuesOf(string file) =>
        DataElements(file).ToDictionary(NameOf, d => (IReadOnlyList<string>)d.Elements("value").Select(v => v.Value).ToArray());

    private static HashSet<int> PlaceholderIndexes(string value) =>
        placeholderRegex.Matches(value).Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();

    /// <summary>
    /// Walks up from the test binaries to the template's own root. A generated project has no
    /// <c>.template.config</c>, and its resources have already been pruned to its configuration.
    /// </summary>
    private static string FindTemplateRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
            return default!;
        }

        return directory.FullName;
    }
}
