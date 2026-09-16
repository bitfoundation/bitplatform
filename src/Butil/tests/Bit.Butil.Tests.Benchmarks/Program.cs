using Bit.Butil.Tests.E2E.Infrastructure;
using ButilTests.Benchmarks;

// The Bit.Butil benchmark suite. Two halves, either of which can be run alone:
//
//   --weight    module download weight, measured off the build artifacts. Deterministic, no browser.
//   --runtime   interop cost and rate limiting, measured in a real browser against the sample app.
//
// No argument runs both. Exits non-zero when a measurement is outside its budget, so the same
// executable serves as the report you read and the check CI runs. See README.md.

var arguments = args.Select(argument => argument.ToLowerInvariant()).ToHashSet();
var runWeight = arguments.Count == 0 || arguments.Contains("--weight");
var runRuntime = arguments.Count == 0 || arguments.Contains("--runtime");

if (arguments.Contains("--help") || arguments.Contains("-h"))
{
    Console.WriteLine("usage: dotnet run [--weight] [--runtime]");
    Console.WriteLine();
    Console.WriteLine("  --weight    module download weight (no browser, exactly reproducible)");
    Console.WriteLine("  --runtime   interop cost and rate limiting (boots the sample app, drives a browser)");
    Console.WriteLine();
    Console.WriteLine("environment:");
    Console.WriteLine("  BUTIL_BENCH_BASE_URL   measure an already-running deployment instead of booting one");
    Console.WriteLine("  BUTIL_E2E_CHANNEL      browser channel to launch, e.g. chrome / msedge");
    Console.WriteLine("  BUTIL_E2E_EXECUTABLE   path to a chromium-family executable");
    Console.WriteLine("  BUTIL_E2E_HEADED       set to 1 to watch the run");
    return 0;
}

var report = new Report();

Console.WriteLine("Bit.Butil benchmarks");
Console.WriteLine("====================");

if (runWeight)
{
    ModuleWeight.Run(report, RepoLayout.ButilProject());
}

if (runRuntime)
{
    // Started only once the weight half has printed, so a failure to boot the app or to find a
    // browser still leaves you with the half that does not need either. Release, because the figure
    // that matters is the one a published build produces; quiet, because the app's startup chatter
    // interleaved with the measurements would make the report unreadable. BUTIL_BENCH_BASE_URL
    // points the run at an already-running deployment instead - which is what you want when the
    // number you care about is a published, minified build rather than the one dotnet run makes.
    await using var app = await SampleAppHost.Start(
        Environment.GetEnvironmentVariable("BUTIL_BENCH_BASE_URL"), configuration: "Release", echoOutput: false);
    await InteropBenchmarks.Run(report, app.BaseUrl);
}

return report.Conclude();
