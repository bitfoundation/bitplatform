using System.Runtime.InteropServices;
using System.Text.Json;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The bytes each fixture app keeps of every bundle, and what the AI package costs an app that lazy-loads it, written to
/// trimmed-bundle-sizes.json in the run's results directory (--results-directory, which CI uploads; TestResults beside the
/// test app otherwise), so the sizes can be followed from one run to the next. It is a record, not a limit: no size fails
/// it, only a publish that failed or a report that no longer reads as the trimming's.
/// </summary>
[TestClass]
public sealed class TrimmedSizeReportTests
{
    public const string FileName = "trimmed-bundle-sizes.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // The publishes the other classes make, with the same properties, so a run of the whole suite publishes nothing more
    // for the record: the server app ServerAppTrimmingTests trims, the WebAssembly app WebAssemblyTrimmingTests does, and
    // the Blazor Web App with a WebAssembly client HostedAppTrimmingTests does.
    private static readonly (string App, string Project, string[] Properties)[] Apps =
    [
        ("server", Publisher.Fixture, ["BitBlazorUITrimAssets=true", "BitBlazorUITrimIcons=true"]),
        ("webassembly", Publisher.Consumer, []),
        ("hosted", Publisher.Hosted, ["BitBlazorUITrimAssets=true"]),
    ];

    [TestMethod]
    public void TheBytesEachFixtureAppKeepsOfEveryBundleShouldBeRecorded()
    {
        var apps = Apps.Select(app =>
        {
            var publish = Publisher.Publish(app.Project, app.Properties);
            publish.AssertSucceeded();

            var bundles = publish.TrimmedBundles;

            // Each app renders the core's components, so its report has at least the core's script and stylesheet.
            Assert.IsTrue(bundles.Any(b => b.Package == "Bit.BlazorUI" && b.Bundle.EndsWith(".js", StringComparison.Ordinal)), $"The {app.App} app's publish reports no core script bundle.");
            Assert.IsTrue(bundles.Any(b => b.Package == "Bit.BlazorUI" && b.Bundle.EndsWith(".css", StringComparison.Ordinal)), $"The {app.App} app's publish reports no core stylesheet.");

            foreach (var bundle in bundles)
            {
                Assert.IsTrue(bundle.KeptPieces <= bundle.AllPieces && bundle.TrimmedBytes <= bundle.FullBytes, $"The {app.App} app's {bundle} keeps more than its package has.");
            }

            // A bundle one publish reports more than once has to say the same each time, and is recorded once.
            foreach (var twice in bundles.GroupBy(b => (b.Package, b.Bundle)).Where(g => g.Count() > 1))
            {
                Assert.AreEqual(1, twice.Distinct().Count(), $"The {app.App} app's publish reports {twice.Key.Package}/{twice.Key.Bundle} differently: {string.Join("; ", twice)}");
            }

            return new
            {
                app.App,
                app.Project,
                app.Properties,
                Bundles = bundles.Distinct().Select(b => new
                {
                    b.Package,
                    b.Bundle,
                    b.KeptPieces,
                    b.AllPieces,
                    b.TrimmedBytes,
                    b.FullBytes,
                    // What is downloaded where the server sends the compressed copies the publish wrote.
                    BrotliBytes = CompressedLength(publish, b, ".br"),
                    GzipBytes = CompressedLength(publish, b, ".gz"),
                }).ToList(),
            };
        }).ToList();

        // What the AI package costs a trimmed WebAssembly app that lazy-loads it (LazyAiTrimmingTests' publish), part by
        // part, with what it downloads as it starts and once its chat opens: the figures the Trimming page states.
        var lazyAi = LazyAiTrimmingTests.Published;
        lazyAi.AssertSucceeded();
        var aiParts = LazyAiTrimmingTests.MeasureAiPackage(lazyAi);

        var report = new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Git("rev-parse", "HEAD"),
            Sdk = Dotnet("--version"),
            Os = RuntimeInformation.OSDescription,
            Configuration = "Release",
            Apps = apps,
            LazyAiPackage = new
            {
                App = "lazy-ai",
                Project = Publisher.LazyAi,
                Parts = aiParts,
                StartupBrotliBytes = aiParts.Where(p => p.When == "startup").Sum(p => p.BrotliBytes),
                ChatBrotliBytes = aiParts.Where(p => p.When == "chat").Sum(p => p.BrotliBytes),
                TotalBrotliBytes = aiParts.Sum(p => p.BrotliBytes),
            },
        };

        var directory = ResultsDirectory();
        System.IO.Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, FileName), JsonSerializer.Serialize(report, Json));
    }

    // The folder the run writes its TRX to: TestContext's own folders are the test app's, or with --report-trx a folder of
    // the run's the platform deletes once it is over.
    private static string ResultsDirectory()
    {
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(arguments, "--results-directory");

        return index >= 0 && index + 1 < arguments.Length ? Path.GetFullPath(arguments[index + 1]) : Path.Combine(AppContext.BaseDirectory, "TestResults");
    }

    private static long? CompressedLength(PublishResult publish, TrimmedBundle bundle, string extension)
    {
        var path = publish.GetAssetPath(bundle.Package, bundle.Bundle) + extension;
        return File.Exists(path) ? new FileInfo(path).Length : null;
    }

    private static string? Dotnet(params string[] arguments) => Read("dotnet", arguments);

    private static string? Git(params string[] arguments) => Read("git", arguments);

    // The first line a command prints, or null where it cannot be run here: the record is still written without it.
    private static string? Read(string fileName, string[] arguments)
    {
        try
        {
            var (exitCode, output) = Publisher.Run(fileName, arguments, Publisher.Root, TimeSpan.FromMinutes(1));
            return exitCode == 0 ? output.Split('\n')[0].Trim() : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or TimeoutException)
        {
            return null;
        }
    }
}
