using Bit.BlazorUI.Tests.Trimming.Infrastructure;

[assembly: global::Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelizeAttribute]

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The wire format of Bit.BlazorUI.AI.Core (BitAiWireSerializer) in a trimmed app and in a native AOT one that reference
/// that package alone, as a server or a plain .NET client does: every built-in
/// BitAiEvent and a request survive a round trip, and the package raises no trim or AOT warning on the way.
/// </summary>
[TestClass]
public sealed class AiWireFormatTrimmingTests
{
    private const string AiWire = "Tests/Bit.BlazorUI.Tests.Trimming.AiWire/Bit.BlazorUI.Tests.Trimming.AiWire.csproj";

    [TestMethod]
    public void TheWireFormatShouldRoundTripInATrimmedApp() => AssertRoundTrips(Publisher.Publish(AiWire, "AiWirePublish=trimmed"));

    [TestMethod]
    public void TheWireFormatShouldRoundTripInANativeAotApp()
    {
        var publish = Publisher.Publish(AiWire, "AiWirePublish=aot");

        // Native AOT links with the platform's toolchain (MSVC on Windows, clang on Linux), which a machine may not have.
        if (publish.ExitCode != 0 && publish.Output.Contains("Platform linker", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive("Native AOT needs the platform linker, which this machine does not have.");
        }

        AssertRoundTrips(publish);
    }

    private static void AssertRoundTrips(PublishResult publish)
    {
        publish.AssertSucceeded();

        var warnings = publish.Output.Split('\n')
            .Where(line => line.Contains("warning IL", StringComparison.Ordinal) && line.Contains("Bit.BlazorUI", StringComparison.Ordinal))
            .Distinct()
            .ToArray();
        Assert.IsEmpty(warnings, "The package raised trim or AOT warnings:\n" + string.Join('\n', warnings));

        var app = Path.Combine(publish.Directory, OperatingSystem.IsWindows() ? "Bit.BlazorUI.Tests.Trimming.AiWire.exe" : "Bit.BlazorUI.Tests.Trimming.AiWire");
        var (exitCode, output) = Publisher.Run(app, [], publish.Directory, TimeSpan.FromMinutes(1));

        Assert.AreEqual(0, exitCode, output);

        // The app counts the samples itself (BitAiWireSamples.All, which this project does not reference), so the count is
        // read rather than pinned: a sample added or removed changes it, and only a failure is one.
        var summary = System.Text.RegularExpressions.Regex.Match(output, @"(\d+) events round-tripped, (\d+) failed");
        Assert.IsTrue(summary.Success, output);
        Assert.IsGreaterThan(0, int.Parse(summary.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), output);
        Assert.AreEqual("0", summary.Groups[2].Value, output);
    }
}
