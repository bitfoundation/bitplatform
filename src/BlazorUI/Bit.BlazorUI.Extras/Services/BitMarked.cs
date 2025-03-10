﻿using System.Globalization;
using Jint;

namespace Bit.BlazorUI;

public class BitMarked
{
    private static string? _markedScriptText;
    private static readonly SemaphoreSlim _markedScriptReadTextSemaphore = new(1, 1);



    public static async Task<string> Parse(string? markdown, CancellationToken cancellationToken)
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
            var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "_content", "Bit.BlazorUI.Extras", "marked", "marked-15.0.7.js");

            if (File.Exists(scriptPath) is false)
            {
                scriptPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "marked", "marked-15.0.7.js");
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
