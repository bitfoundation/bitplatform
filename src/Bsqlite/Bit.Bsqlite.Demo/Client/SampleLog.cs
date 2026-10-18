using System.Collections;
using System.Reflection;

namespace Bit.Bsqlite.Demo.Client;

/// <summary>Writes the full exception graph to the browser console.</summary>
internal static class SampleLog
{
    public static void Exception(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(exception, depth: 0);
    }

    private static void Write(Exception exception, int depth)
    {
        var prefix = depth == 0 ? "[Bsqlite sample]" : new string(' ', depth * 2);
        Console.Error.WriteLine($"{prefix} {exception.GetType().FullName}: {exception.Message}");
        Console.Error.WriteLine($"{prefix} HResult=0x{exception.HResult:X8} Source={exception.Source}");
        if (!string.IsNullOrEmpty(exception.HelpLink))
        {
            Console.Error.WriteLine($"{prefix} HelpLink={exception.HelpLink}");
        }

        foreach (DictionaryEntry entry in exception.Data)
        {
            Console.Error.WriteLine($"{prefix} Data[{entry.Key}]={entry.Value}");
        }

        if (exception.StackTrace is { Length: > 0 } stack)
        {
            Console.Error.WriteLine(stack);
        }

        switch (exception)
        {
            case AggregateException aggregate:
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    Write(inner, depth + 1);
                }

                return;
            case ReflectionTypeLoadException load:
                foreach (var loader in load.LoaderExceptions)
                {
                    if (loader is not null)
                    {
                        Write(loader, depth + 1);
                    }
                }

                break;
        }

        if (exception.InnerException is not null && exception is not AggregateException)
        {
            Console.Error.WriteLine($"{prefix} Inner exception:");
            Write(exception.InnerException, depth + 1);
        }
    }
}
