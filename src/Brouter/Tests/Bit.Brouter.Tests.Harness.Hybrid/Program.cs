using Microsoft.Web.WebView2.Core;

namespace Bit.Brouter.Tests.Harness.Hybrid;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // The E2E suite starts this app without a visible console and waits on WebView2's debugging port.
        // Anything that stops the WebView from starting must end the process with the reason on stderr,
        // which the suite quotes - never park it behind WinForms' unhandled exception dialog.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Fail("Unhandled exception", e.ExceptionObject);

        string runtimeVersion;
        try
        {
            runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Fail("The WebView2 Runtime is not installed", ex);
            return 1;
        }
        Console.Error.WriteLine($"WebView2 Runtime {runtimeVersion}");

        ApplicationConfiguration.Initialize();
        Application.Run(new HarnessForm(StartPathFrom(args)));
        return 0;
    }

    internal static void Fail(string reason, object? exception)
    {
        Console.Error.WriteLine($"{reason}: {exception}");
        Console.Error.Flush();
        Environment.Exit(1);
    }

    // --start-path /items/9 opens the WebView at a deep path, the hybrid counterpart of a deep link.
    private static string StartPathFrom(string[] args)
    {
        var index = Array.IndexOf(args, "--start-path");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : "/";
    }
}
