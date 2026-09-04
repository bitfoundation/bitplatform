namespace Boilerplate.Tests.Features.Culture;

/// <summary>
/// Adding (or removing) a culture touches three lists in three subsystems that cannot share code - an attribute
/// argument must be a constant and the translator's configuration is JSON - so they are bound together here instead:
/// <list type="number">
/// <item><c>CultureInfoManager.SupportedCultures</c> - what the app itself supports.</item>
/// <item>Android's <c>MainActivity</c> <c>DataPathPrefixes</c> - the culture-prefixed app-link paths, in BOTH the
/// canonical and the lowercased casing. Miss one and every <c>https://your-app/xx-XX/...</c> link - the only form the
/// server mints for that culture (See <c>UseCultureUrlRedirection</c>) - silently opens in the phone's browser
/// instead of the installed app. No error anywhere; it just looks like a marketing problem.</item>
/// <item><c>Bit.ResxTranslator.json</c>'s <c>SupportedLanguages</c> - without the language there, the CD pipelines
/// deploy that culture with untranslated resources.</item>
/// </list>
/// Unlike most source-inspecting tests in this suite, this one deliberately runs in generated projects as well:
/// that is where the twelfth language actually gets added. The repository root is therefore located by
/// <c>Bit.ResxTranslator.json</c> (which every generated project ships) rather than by <c>.template.config</c>,
/// and the MAUI project is found by pattern because generation renames it.
/// </summary>
[TestClass, TestCategory("UnitTest"), TestCategory("Localization")]
public partial class SupportedCultureContractTests
{
    [TestMethod]
    public void EverySupportedCulture_Should_AppearInMainActivityAndTranslatorConfig()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("On an invariant globalization build the culture lists are dormant; there is nothing to keep in step.");
        }

        var repositoryRoot = FindRepositoryRoot();
        var cultureNames = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture.Name).ToArray();

        Assert.IsGreaterThan(0, cultureNames.Length, "SupportedCultures parsed to zero cultures - the rest of this test would be vacuous.");

        // ---- MainActivity's DataPathPrefixes ----

        var mainActivityFile = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "Client"), "MainActivity.cs", SearchOption.AllDirectories)
            .SingleOrDefault(file => file.Replace('\\', '/').Contains("/Platforms/Android/", StringComparison.Ordinal));

        Assert.IsNotNull(mainActivityFile, "No Android MainActivity.cs was found under src/Client - the app-link prefixes cannot be verified.");

        var mainActivity = File.ReadAllText(mainActivityFile);
        List<string> missingPrefixes = [];

        foreach (var cultureName in cultureNames)
        {
            // Both casings, because Android matches pathPrefix case-sensitively and links arrive in both forms
            // (the server canonicalizes to /fa-IR, humans type /fa-ir).
            foreach (var prefix in new[] { $"\"/{cultureName}\"", $"\"/{cultureName.ToLowerInvariant()}\"" })
            {
                if (mainActivity.Contains(prefix, StringComparison.Ordinal) is false)
                    missingPrefixes.Add(prefix);
            }
        }

        Assert.IsEmpty(missingPrefixes,
            $"{Path.GetFileName(mainActivityFile)}'s DataPathPrefixes is missing: {string.Join(", ", missingPrefixes)}. " +
            "Without them, culture-prefixed app links of those cultures open in the browser instead of the installed Android app.");

        // ---- Bit.ResxTranslator.json's SupportedLanguages ----

        using var translatorConfig = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "Bit.ResxTranslator.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        var defaultLanguage = translatorConfig.RootElement.GetProperty("DefaultLanguage").GetString();

        var supportedLanguages = translatorConfig.RootElement.GetProperty("SupportedLanguages")
            .EnumerateArray().Select(language => language.GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingLanguages = cultureNames
            .Select(cultureName => cultureName.Split('-')[0])
            .Where(language => string.Equals(language, defaultLanguage, StringComparison.OrdinalIgnoreCase) is false)
            .Distinct()
            .Where(language => supportedLanguages.Contains(language) is false)
            .ToArray();

        Assert.IsEmpty(missingLanguages,
            $"Bit.ResxTranslator.json's SupportedLanguages is missing: {string.Join(", ", missingLanguages)}. " +
            "Without them, the CD pipelines deploy those cultures with untranslated resources.");
    }

    /// <summary>
    /// Walks up from the test binaries to the directory holding <c>Bit.ResxTranslator.json</c> - the repository root
    /// of the template's own tree AND of every generated project, which is what lets this test run in both.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bit.ResxTranslator.json")))
                return directory.FullName;
        }

        throw new AssertFailedException("No Bit.ResxTranslator.json above the test binaries; the repository root could not be located.");
    }
}
