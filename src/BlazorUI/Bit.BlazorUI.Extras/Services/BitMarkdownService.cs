using System.Globalization;
using Jint;

namespace Bit.BlazorUI;

/// <summary>
/// A utility service to parse Markdown texts into html strings. Works smoothly in both server and client.
/// </summary>
public class BitMarkdownService(IJSRuntime js)
{
    private const string MARKED_FILE = "marked/marked-15.0.7.js";



    private static string? _markedScriptText;
    private static readonly SemaphoreSlim _markedScriptReadTextSemaphore = new(1, 1);



    public async Task<string> Parse(string? markdown, CancellationToken cancellationToken)
    {
        if (markdown.HasNoValue()) return string.Empty;

        var html = string.Empty;

        if (js.IsRuntimeInvalid()) // server (prerendering)
        {
            try
            {
                html = await Task.Run(async () =>
                {
                    return await RuntJint(markdown, cancellationToken);
                }, cancellationToken);
            }
            catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Jint") is true)
            {
                Console.Error.WriteLine("Please install `Jint` nuget package on the server project.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        else // client
        {
            var scriptPath = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";
            if ((await js.BitMarkdownViewerCheckScriptLoaded(scriptPath)) is false)
            {
                await js.BitExtrasInitScripts([scriptPath]);
            }

            html = await js.BitMarkdownViewerParse(markdown!);
        }

        return html;
    }



    private static async Task<string> RuntJint(string? markdown, CancellationToken cancellationToken)
    {
        if (markdown.HasNoValue()) return string.Empty;

        await ReadMarkedScriptText(cancellationToken);
        if (_markedScriptText.HasNoValue()) return string.Empty;

        using var engine = new Engine(options =>
        {
            options.Strict();
            options.CancellationToken(cancellationToken);
            options.Culture(CultureInfo.CurrentUICulture);
        }).Execute(_markedScriptText!);

        var fn = engine.Evaluate("marked.parse").AsFunctionInstance();

        return fn.Call(markdown).AsString();
    }

    private static async Task<string> ReadMarkedScriptText(CancellationToken cancellationToken)
    {
        if (_markedScriptText is not null) return _markedScriptText;

        try
        {
            await _markedScriptReadTextSemaphore.WaitAsync(cancellationToken);
            if (_markedScriptText is not null) return _markedScriptText;

            //TODO: this script path discovery needs improvement!
            var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "_content", "Bit.BlazorUI.Extras", MARKED_FILE);

            if (File.Exists(scriptPath) is false)
            {
                scriptPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", MARKED_FILE);
            }

            if (File.Exists(scriptPath) is false)
            {
                Console.Error.WriteLine("Could not find the marked js script file!");
                return _markedScriptText = string.Empty;
            }

            return _markedScriptText = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        }
        finally
        {
            _markedScriptReadTextSemaphore.Release();
        }
    }
}
