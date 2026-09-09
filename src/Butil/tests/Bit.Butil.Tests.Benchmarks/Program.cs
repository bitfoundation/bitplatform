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
    ModuleWeight.Run(report, ButilProjectDirectory());
}

if (runRuntime)
{
    // Started only once the weight half has printed, so a failure to boot the app or to find a
    // browser still leaves you with the half that does not need either.
    await using var app = new SampleAppFixture();
    await app.Start();
    await InteropBenchmarks.Run(report, app.BaseUrl);
}

return report.Conclude();

// The Bit.Butil project folder, found by walking up from the output directory rather than by a
// relative path, so the run does not depend on the working directory the shell or IDE picked.
static string ButilProjectDirectory()
{
    var directory = AppContext.BaseDirectory;
    for (var i = 0; i < 10 && directory is not null; i++)
    {
        var candidate = Path.Combine(directory, "Bit.Butil");
        if (File.Exists(Path.Combine(candidate, "build.mjs"))) return candidate;
        directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
    }

    throw new DirectoryNotFoundException("Could not locate the Bit.Butil project folder by walking up from the output directory.");
}
