using Jint;
using System.Reflection;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a SEO friendly Blazor wrapper around the famous markedjs library.
/// <see href="https://github.com/markedjs/marked"/>
/// </summary>
public partial class BitMarkdownViewer : BitComponentBase
{
    private string? _html;
    private CancellationTokenSource _cts = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The Markdown string value to render as an html element.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnMarkdownSet))]
    public string? Markdown { get; set; }



    protected override string RootElementClass => "bit-mdv";

    protected override async Task OnInitializedAsync()
    {
        if (_js.IsRuntimeInvalid()) // prerendering
        {
            try
            {
                await RunJint();
            }
            catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Jint") is true)
            {
                Console.Error.WriteLine("Please install `Jint` nuget package on your SERVER project.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        else
        {
            var scriptPath = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";
            if (await _js.BitMarkdownViewerCheckScript(scriptPath) is false)
            {
                await _js.BitExtrasInitScripts([scriptPath]);
            }

            if (_html.HasNoValue())
            {
                await ParseAndRender();
            }
        }

        await base.OnInitializedAsync();
    }

    private static string? _script;
    private static SemaphoreSlim _semaphore = new(1, 1);
    private async Task<string> GetMarkedJSScript()
    {
        if (_script is not null)
            return _script;
        try
        {
            await _semaphore.WaitAsync();
            if (_script is not null)
                return _script;
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "marked", "marked-15.0.7.js");
            if (File.Exists(scriptPath) is false)
            {
                var envType = Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.AspNetCore.Hosting.Abstractions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60")!;
                var fileProviderProp = envType.GetProperty("ContentRootFileProvider", BindingFlags.Instance | BindingFlags.Public)!;
                var webRootPathProp = envType.GetProperty("WebRootPath")!;
                var env = _serviceProvider.GetRequiredService(envType);
                var webRootPath = (string)webRootPathProp.GetValue(env)!;
                scriptPath = Path.Combine(webRootPath, "marked", "marked-15.0.7.js");
            }
            return _script = await File.ReadAllTextAsync(scriptPath);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RunJint()
    {
        if (Markdown.HasNoValue()) return;

        await Task.Run(async () =>
        {
            using var engine = new Engine(options =>
            {
                options.Strict();
                options.CancellationToken(_cts.Token);
                options.Culture(CultureInfo.CurrentUICulture);
            }).Execute(await GetMarkedJSScript());

            var fn = engine.Evaluate("marked.parse").AsFunctionInstance();

            _html = fn.Call(Markdown).AsString();

            await InvokeAsync(StateHasChanged);

        }, _cts.Token);
    }

    private async Task OnMarkdownSet()
    {
        if (IsRendered is false) return;

        await ParseAndRender();
    }

    private async Task ParseAndRender()
    {
        if (Markdown.HasNoValue()) return;

        _html = await _js.BitMarkdownViewerParse(Markdown!);

        StateHasChanged();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _cts.Cancel();
        _cts.Dispose();

        await base.DisposeAsync(disposing);
    }
}
