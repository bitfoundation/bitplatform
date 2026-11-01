namespace Boilerplate.WorkbenchTests.Features.EditText;

/// <summary>
/// Edit text mode traces a text on the page back to its resource entry and saves a new text into the translation files,
/// as long as the page can still show it.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchTextTests
{
    private static WorkbenchTextEdits TextEdits(bool isLocal = true)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IHttpContextAccessor>(new FixedHttpContextAccessor(isLocal));
        services.AddSingleton<WorkbenchTextEdits>();

        return services.BuildServiceProvider().GetRequiredService<WorkbenchTextEdits>();
    }

    [TestMethod]
    [DataRow(nameof(WorkbenchStrings.BuildingsSaved), "{0} was saved", true)]
    [DataRow(nameof(WorkbenchStrings.BuildingsSaved), "{0}, {0} again", true)]
    [DataRow(nameof(WorkbenchStrings.BuildingsSaved), "Saved", false)]
    [DataRow(nameof(WorkbenchStrings.BuildingsSaved), "{1} was saved", false)]
    [DataRow(nameof(WorkbenchStrings.BuildingsSaved), "{0} was {saved", false)]
    [DataRow(nameof(WorkbenchStrings.Save), "Store", true)]
    [DataRow(nameof(WorkbenchStrings.Save), "Store {0}", false)]
    public void ANewText_Should_KeepThePlaceholdersOfTheOriginal(string key, string text, bool keeps)
    {
        Assert.AreEqual(keeps, TextEdits().KeepsPlaceholders(nameof(WorkbenchStrings), key, text));
    }

    [TestMethod]
    public async Task SavingFromAnotherMachineOrWithoutTheProject_Should_BeRefused()
    {
        Assert.IsFalse(await TextEdits(isLocal: false).Save(nameof(WorkbenchStrings), nameof(WorkbenchStrings.Save), CultureInfoManager.DefaultCulture.Name, "Store"));

        // The tests run from their own output, where no project with translation files is above them.
        Assert.IsFalse(TextEdits().CanWriteFiles);
        Assert.IsFalse(await TextEdits().Save(nameof(WorkbenchStrings), nameof(WorkbenchStrings.Save), CultureInfoManager.DefaultCulture.Name, "Store"));
        Assert.IsFalse(await TextEdits().Save(nameof(WorkbenchStrings), nameof(WorkbenchStrings.BuildingsSaved), CultureInfoManager.DefaultCulture.Name, "Saved"));
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void Translations_Should_ListTheTextInEveryLanguage()
    {
        var translations = TextEdits().Translations(nameof(WorkbenchStrings), nameof(WorkbenchStrings.Save));

        Assert.HasCount(CultureInfoManager.SupportedCultures.Length, translations);
        Assert.AreEqual(WorkbenchStrings.Save, translations.Single(t => t.Culture == CultureInfoManager.DefaultCulture.Name).Value);
        Assert.IsEmpty(TextEdits().Translations("NoSuchStrings", nameof(WorkbenchStrings.Save)));
    }

    [TestMethod]
    public void APickedText_Should_LeadBackToItsEntry()
    {
        var recorder = new WorkbenchTextRecorder();
        var culture = CultureInfo.CurrentUICulture.Name;

        recorder.Record(nameof(WorkbenchStrings), nameof(WorkbenchStrings.BuildingsSaved), "{0} is saved", "Rijksmuseum is saved");
        recorder.Record(nameof(WorkbenchStrings), nameof(WorkbenchStrings.Save), "Save", "Save");

        Assert.AreEqual(nameof(WorkbenchStrings.BuildingsSaved), recorder.Find("Rijksmuseum is saved", culture).Single().Key);
        Assert.AreEqual(nameof(WorkbenchStrings.BuildingsSaved), recorder.Find("Euromast is saved", culture).Single().Key, "A format matches with any value.");
        Assert.AreEqual(nameof(WorkbenchStrings.Save), recorder.Find("  Save \n", culture).Single().Key, "Whitespace doesn't count.");
        Assert.AreEqual(nameof(WorkbenchStrings.Save), recorder.Find("Save changes", culture).Single().Key, "A text that contains an entry's leads to it.");
        Assert.IsEmpty(recorder.Find("Rijksmuseum is saved", "xx-XX"));
        Assert.IsEmpty(recorder.Find("   ", culture));
    }
}
