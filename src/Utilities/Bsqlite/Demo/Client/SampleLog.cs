using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.Bsqlite.Demo.Client;

/// <summary>Writes the full exception graph to the browser console.</summary>
/// <remarks>
/// Through an <see cref="ILogger"/>, which writes with <c>console.error</c>, rather than to
/// <see cref="Console.Error"/>: on WebAssembly anything written to stderr also raises Blazor's "An
/// unhandled error has occurred" bar, and every exception handed to this has been handled - most of them
/// the very failures the pages invite a reader to cause.
/// </remarks>
internal static class SampleLog
{
    /// <summary>
    /// Set by Program.cs once the WebAssembly host is built. Until then, and in the prerender pass on the
    /// server, the graph goes to stdout.
    /// </summary>
    public static ILogger? Logger { get; set; }

    public static void Exception(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var text = new StringBuilder();
        Write(text, exception, depth: 0);

        if (Logger is { } logger)
        {
            logger.LogError("{ExceptionGraph}", text.ToString().TrimEnd());
        }
        else
        {
            Console.Write(text);
        }
    }

    private static void Write(StringBuilder text, Exception exception, int depth)
    {
        var prefix = depth == 0 ? "[Bsqlite sample]" : new string(' ', depth * 2);
        text.AppendLine($"{prefix} {exception.GetType().FullName}: {exception.Message}");
        text.AppendLine($"{prefix} HResult=0x{exception.HResult:X8} Source={exception.Source}");
        if (!string.IsNullOrEmpty(exception.HelpLink))
        {
            text.AppendLine($"{prefix} HelpLink={exception.HelpLink}");
        }

        foreach (DictionaryEntry entry in exception.Data)
        {
            text.AppendLine($"{prefix} Data[{entry.Key}]={entry.Value}");
        }

        if (exception.StackTrace is { Length: > 0 } stack)
        {
            text.AppendLine(stack);
        }

        switch (exception)
        {
            case AggregateException aggregate:
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    Write(text, inner, depth + 1);
                }

                return;
            case ReflectionTypeLoadException load:
                foreach (var loader in load.LoaderExceptions)
                {
                    if (loader is not null)
                    {
                        Write(text, loader, depth + 1);
                    }
                }

                break;
        }

        if (exception.InnerException is not null && exception is not AggregateException)
        {
            text.AppendLine($"{prefix} Inner exception:");
            Write(text, exception.InnerException, depth + 1);
        }
    }
}
