using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Jint;

namespace Bit.BlazorUI;

/// <summary>
/// A utility service to parse Markdown texts into html strings. Works smoothly in both server and client.
/// </summary>
public class BitMarkdownService(IJSRuntime js, IServiceProvider serviceProvider)
{
    private const string MARKED_FILE = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";



    private static string? _markedScriptContent;
    private static readonly SemaphoreSlim _markedScriptReadSemaphore = new(1, 1);



    public async Task<string> Parse(string? markdown, CancellationToken cancellationToken)
    {
        if (markdown.HasNoValue()) return string.Empty;

        var html = string.Empty;

        if (js.IsRuntimeInvalid()) // server (prerendering)
        {
            try
            {
                html = await Task.Run(async () => await RunJint(markdown, cancellationToken), cancellationToken);
            }
            catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Jint") is true)
            {
                Console.Error.WriteLine("Please install `Jint` NuGet package on the server project.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        else // client
        {
            if ((await js.BitMarkdownViewerCheckScriptLoaded(MARKED_FILE)) is false)
            {
                await js.BitExtrasInitScripts([MARKED_FILE]);
            }

            html = await js.BitMarkdownViewerParse(markdown!);
        }

        return html;
    }



    private async Task<string> RunJint(string? markdown, CancellationToken cancellationToken)
    {
        if (markdown.HasNoValue()) return string.Empty;

        await ReadMarkedScriptContent(cancellationToken);
        if (_markedScriptContent.HasNoValue()) return string.Empty;

        using var engine = new Engine(options =>
        {
            options.Strict();
            options.CancellationToken(cancellationToken);
            options.Culture(CultureInfo.CurrentUICulture);
        }).Execute(_markedScriptContent!);

        var fn = engine.Evaluate("marked.parse").AsFunctionInstance();

        return fn.Call(markdown).AsString();
    }

    private async Task<string> ReadMarkedScriptContent(CancellationToken cancellationToken)
    {
        if (_markedScriptContent is not null) return _markedScriptContent;

        try
        {
            await _markedScriptReadSemaphore.WaitAsync(cancellationToken);

            if (_markedScriptContent is not null) return _markedScriptContent;

            var env = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

            var fileInfo = env.WebRootFileProvider.GetFileInfo(MARKED_FILE);

            if (fileInfo.Exists is false)
            {
                Console.Error.WriteLine("Could not find the marked js script file.");
                return _markedScriptContent = string.Empty;
            }

            using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            return _markedScriptContent = await reader.ReadToEndAsync(cancellationToken);
        }
        finally
        {
            _markedScriptReadSemaphore.Release();
        }
    }
}
