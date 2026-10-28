namespace Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.BlazorHeadlessUI.Demo.Server published the way blazorheadlessui.demo.cd.yml
/// publishes it, started on a free port, once for the whole run. Every test opens its own browser context (its own
/// storage and WebAssembly instance, or its own circuit), so sharing the process shares nothing.
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

    /// <summary>The last lines the demo server wrote, for failure messages; empty for an external demo.</summary>
    public string RecentOutput => _process?.RecentOutput ?? string.Empty;

    public static async Task<DemoHost> StartAsync()
    {
        if (E2EEnvironment.BaseUrl is { } external)
        {
            await ExpectRenderModeAsync(external);
            return new DemoHost(external, process: null);
        }

        var directory = E2EEnvironment.DemoDirectory ?? await PublishAsync();
        var assembly = RepoLayout.DemoServerAssembly(directory);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException($"{directory} holds no published demo server. Publish it with 'dotnet publish " +
                                            $"src/BlazorHeadlessUI/Demo/{RepoLayout.DemoServerName}/{RepoLayout.DemoServerName}.csproj -c Release -o <folder>'.", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // The publish folder is the content root (wwwroot, the static assets manifest), and Production is what the site
        // runs in - whatever the machine's own ASPNETCORE_ENVIRONMENT says.
        var process = ChildProcess.Start(DotNet, [assembly, "--urls", baseUrl], directory,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
            await ExpectRenderModeAsync(baseUrl);
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
    /// blazorheadlessui.demo.cd.yml's publish (less its version stamp), into a folder next to the test binaries. The
    /// Server mode leaves the WebAssembly client out (<c>-p:IncludeWasm=false</c>), and App.razor then renders every page
    /// on a circuit. Run from the demo's folder so src/global.json picks the SDK, as it does for CI's step.
    /// </summary>
    private static async Task<string> PublishAsync()
    {
        var project = RepoLayout.DemoServerProject();
        var output = RepoLayout.PublishDirectory(E2EEnvironment.RenderMode);

        List<string> arguments =
        [
            "publish", project, "-c", "Release", "-p:AspNetCoreHostingModel=OutOfProcess", "-o", output,
            "-p:InvariantGlobalization=true", "-nologo", "-v:m",
        ];
        if (E2EEnvironment.IsWebAssembly is false) arguments.Add("-p:IncludeWasm=false");

        await ChildProcess.RunToCompletionAsync(DotNet, arguments, Path.GetDirectoryName(project)!, TimeSpan.FromMinutes(20));
        return output;
    }

    /// <summary>
    /// Fails the run up front when the demo renders in another mode than the one asked for - a publish folder or a
    /// running demo handed in from outside, made the other way - rather than letting every test pass against the wrong
    /// runtime. Each interactive root of a prerendered page is preceded by a marker naming its render mode.
    /// </summary>
    private static async Task ExpectRenderModeAsync(string baseUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var html = await client.GetStringAsync(baseUrl + "/");
        var marker = $"<!--Blazor:{{\"type\":\"{(E2EEnvironment.IsWebAssembly ? "webassembly" : "server")}\"";
        if (html.Contains(marker, StringComparison.Ordinal) is false)
            throw new InvalidOperationException($"The demo at {baseUrl} does not render in {E2EEnvironment.RenderMode} mode (no {marker} marker on its home page). " +
                                                "Set BLAZORHEADLESSUI_E2E_RENDER_MODE to the mode it was published for.");
    }
}
