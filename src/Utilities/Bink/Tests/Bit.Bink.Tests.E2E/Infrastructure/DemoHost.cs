namespace Bit.Bink.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.Bink.Demo.Server published the way cd.bink.demo.yml publishes it, started on
/// a free port, once for the whole run. The demo keeps no state on the server (each page runs in the browser on
/// WebAssembly), and every test opens its own browser context, so sharing the process shares nothing.
/// </summary>
/// <remarks>
/// A publish, not a build output: only a publish trims the WebAssembly client, and an interop payload the trimmer
/// breaks works in every build and fails only there (src/CLAUDE.md).
/// </remarks>
public sealed class DemoHost : IAsyncDisposable
{
    private readonly ChildProcess? _process;

    private DemoHost(string baseUrl, ChildProcess? process)
    {
        BaseUrl = baseUrl;
        _process = process;
    }

    /// <summary>The URL the demo answers at, without a trailing slash.</summary>
    public string BaseUrl { get; }

    public static async Task<DemoHost> StartAsync()
    {
        if (E2EEnvironment.BaseUrl is { } external)
            return new DemoHost(external, process: null);

        var directory = E2EEnvironment.DemoDirectory ?? await PublishAsync();
        var assembly = RepoLayout.DemoServerAssembly(directory);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException($"{directory} holds no published demo server. Publish it with 'dotnet publish " +
                                            $"src/Utilities/Bink/Demo/Server/{RepoLayout.DemoServerName}.csproj -c Release -o <folder>'.", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // The publish folder is the content root (wwwroot, the static assets manifest), and Production is what the site
        // runs in - whatever the machine's own ASPNETCORE_ENVIRONMENT says.
        var process = ChildProcess.Start(DotNet, [assembly, "--urls", baseUrl], directory,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new DemoHost(baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>The dotnet the run was started with where the SDK says so, else the one on the PATH.</summary>
    private static string DotNet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    /// <summary>
    /// cd.bink.demo.yml's publish (less its version stamp), into a folder next to the test binaries. Run from the
    /// demo's folder so src/global.json picks the SDK, as it does for CI's step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A publish whose stamp lists the same files it is made from and is newer than every one of them is used as it
    /// is, so re-running a test does not pay for a trimmed WebAssembly publish each time; <c>BINK_E2E_REPUBLISH=1</c>
    /// publishes all the same. Otherwise the folder is emptied first: a publish leaves what an earlier one wrote with
    /// other settings (fingerprinted assets, compressed copies, assemblies since removed) next to its own, and the
    /// server would serve those.
    /// </para>
    /// <para>
    /// The stamp is written only when no input was added, removed or changed while the publish ran: the publish may
    /// have read the file before the edit, and the edit, older than a stamp written after it, would go unnoticed.
    /// </para>
    /// <para>
    /// Nothing outlives the publish: no MSBuild node, no compiler or Razor server. One left running holds the output
    /// pipes and the demo's obj and bin files open long after the publish is done.
    /// </para>
    /// </remarks>
    private static async Task<string> PublishAsync()
    {
        var project = RepoLayout.DemoServerProject();
        var output = RepoLayout.PublishDirectory();
        string[] arguments = ["publish", project, "-c", "Release", "-p:AspNetCoreHostingModel=OutOfProcess", "-o", output,
                              "-p:InvariantGlobalization=true", "-nologo", "-v:m",
                              "-nodeReuse:false", "-p:UseSharedCompilation=false", "-p:UseRazorBuildServer=false"];

        var stamp = Path.Combine(output, PublishStampName);
        var inputs = SnapshotInputs();
        if (E2EEnvironment.Republish is false && IsUpToDate(stamp, StampText(arguments, inputs), inputs)) return output;

        if (Directory.Exists(output)) Directory.Delete(output, recursive: true);

        await ChildProcess.RunToCompletionAsync(DotNet, arguments, Path.GetDirectoryName(project)!, TimeSpan.FromMinutes(20),
            new Dictionary<string, string> { ["MSBUILDDISABLENODEREUSE"] = "1", ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0" });

        // Left unmarked, the publish is redone by the next run.
        if (SnapshotInputs().SequenceEqual(inputs))
            await File.WriteAllTextAsync(stamp, StampText(arguments, inputs));

        return output;
    }

    private const string PublishStampName = ".bink-e2e-publish";

    /// <summary>Every input of the publish with its last write time, in a stable order.</summary>
    private static KeyValuePair<string, DateTime>[] SnapshotInputs() =>
        [.. RepoLayout.DemoInputs().Order(StringComparer.Ordinal).Select(input => KeyValuePair.Create(input, File.GetLastWriteTimeUtc(input)))];

    /// <summary>The publish's arguments, then the path of every file it is made from, one per line.</summary>
    private static string StampText(string[] arguments, KeyValuePair<string, DateTime>[] inputs) =>
        string.Join('\n', inputs.Select(input => input.Key).Prepend(string.Join(' ', arguments)));

    /// <summary>
    /// True when <paramref name="stamp"/> records the same publish of the same files and is newer than every one of them.
    /// </summary>
    private static bool IsUpToDate(string stamp, string stampText, KeyValuePair<string, DateTime>[] inputs)
    {
        if (File.Exists(stamp) is false || File.ReadAllText(stamp) != stampText) return false;

        var published = File.GetLastWriteTimeUtc(stamp);
        return inputs.All(input => input.Value < published);
    }
}
