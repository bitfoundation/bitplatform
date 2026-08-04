//+:cnd:noEmit
using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Tests.Features.Products;

/// <summary>
/// Two culture bugs in the catalog that a machine set to en-US can never see, which is why they survived so long.
/// Neither needs a server; both need the ambient culture swapped, so they run without <c>AppTestServer</c>.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public partial class ProductCultureSafetyTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// <c>ProductConfiguration</c> parses its seed base date from a literal, and <c>Configure</c> runs inside
    /// <c>OnModelCreating</c> - on whatever culture happens to be ambient. Without an explicit
    /// <c>CultureInfo.InvariantCulture</c> that literal <b>throws</b> under <c>ar-SA</c> (UmAlQura calendar), which
    /// takes down <c>dotnet watch</c>, <c>dotnet ef migrations add</c> and this very test assembly's initialization on
    /// any machine with that OS locale.
    /// <para>
    /// Building the model is the only honest way to assert it: that is the exact call path that fails, and it covers
    /// any other culture-sensitive parse a future entity configuration introduces. The fa-IR half of the same bug -
    /// where the parse <i>succeeds</i> and yields the year 2643 - is covered by the source-level test below, because
    /// detecting a wrong-but-valid value would mean reaching into EF's internal seed-data API.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("ar-SA")] // UmAlQura - "2022-07-12" is not a valid date at all in this calendar.
    [DataRow("fa-IR")] // Persian.
    [DataRow("th-TH")] // Buddhist - not a supported UI culture, but a perfectly ordinary OS locale.
    public void BuildingTheModel_Should_NotThrowUnderANonGregorianCulture(string cultureName)
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("Invariant globalization collapses every culture, so there is no calendar to disagree about.");
        }

        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            // Sqlite needs no connection to build a model, and the entity configuration under test is provider
            // independent. Touching Model is what runs OnModelCreating, and therefore ProductConfiguration.Configure.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var dbContext = new AppDbContext(options);

            Assert.IsNotNull(dbContext.Model,
                $"The EF model must build under '{cultureName}'. Entity configuration must never read " +
                $"CultureInfo.CurrentCulture: OnModelCreating runs on the ambient culture of whoever started the process.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// The other half of the same defect, and the reason it is asserted against the source text rather than against
    /// behaviour: under <c>fa-IR</c> the seed literal parses <b>successfully</b>, into the year 2643. Nothing throws,
    /// the rows are simply wrong - and they are then baked into whatever migration that developer generates, which is
    /// what ships. Catching that at runtime would mean reading EF's seed data through an internal API; asserting that
    /// the call site names a format provider costs nothing and is the invariant we actually want to hold.
    /// <para>
    /// Runs against the template's own working copy; a generated project reports inconclusive rather than failing.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryDateParseInEntityConfiguration_Should_NameAFormatProvider()
    {
        var configurationsRoot = FindTemplateRoot();

        if (configurationsRoot is null)
        {
            Assert.Inconclusive("No .template.config above the test binaries - this is a generated project, not the template's own tree.");
            return;
        }

        var serverApi = Path.Combine(configurationsRoot, "src", "Server", "Boilerplate.Server.Api");
        var unprovidered = new Regex(@"Date(?:Time|TimeOffset)\.Parse(?:Exact)?\(", RegexOptions.Compiled);

        List<string> offenders = [];

        foreach (var file in Directory.EnumerateFiles(serverApi, "*Configuration.cs", SearchOption.AllDirectories))
        {
            var lineNumber = 0;

            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;

                if (unprovidered.IsMatch(line) is false)
                    continue;

                // InvariantCulture is the only provider that is right here, so it is the only one accepted. Seed data
                // is fixed literals: there is no reader whose locale it should follow. CultureInfo.CurrentCulture is
                // rejected rather than treated as "a provider was named" - naming the ambient culture explicitly is
                // exactly as locale-dependent as omitting the argument, which is the bug this test exists for.
                if (line.Contains("InvariantCulture", StringComparison.Ordinal))
                    continue;

                offenders.Add($"{Path.GetRelativePath(configurationsRoot, file)}:{lineNumber} -> {line.Trim()}");
            }
        }

        Assert.IsEmpty(offenders,
            "Entity configuration runs inside OnModelCreating, on whatever culture the process happens to be in, so a " +
            "date parsed there must name CultureInfo.InvariantCulture. Omitting the provider - or passing " +
            "CultureInfo.CurrentCulture, or a variable holding one - makes the seed values depend on the developer's " +
            "OS locale:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// <c>FormattedPrice</c>'s right-to-left arm exists to put the currency symbol after the number. It must not also
    /// change the amount - a hard coded <c>N0</c> rounded 19.99 up to 20 for RTL viewers only, while every other
    /// culture saw 19.99. <c>fa-IR</c> is the control: the Rial genuinely has zero currency decimal digits, so its
    /// output is expected to stay rounded, and a fix that blanket-applied two decimals would be wrong there.
    /// </summary>
    [TestMethod]
    public void FormattedPrice_Should_KeepTheCulturesOwnPrecision()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("FormatPrice takes its invariant branch, which has no right-to-left arm to test.");
        }

        var product = new ProductDto { Price = 19.99M };

        Assert.Contains("19", FormattedPriceUnder(product, "en-US"), "Control: a left-to-right culture keeps the cents.");

        Assert.Contains("19", FormattedPriceUnder(product, "ar-SA"),
            "ar-SA has two currency decimal digits, so the fractional part must survive. A '20' here means the " +
            "right-to-left arm is still rounding money away for Arabic viewers only.");

        Assert.Contains("20", FormattedPriceUnder(product, "fa-IR"),
            "fa-IR's Rial has zero currency decimal digits, so rounding is correct there - the fix has to follow the " +
            "culture rather than hard code a precision in either direction.");
    }

    private static string FormattedPriceUnder(ProductDto product, string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            return product.FormattedPrice;
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static string? FindTemplateRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        return directory?.FullName;
    }
}
