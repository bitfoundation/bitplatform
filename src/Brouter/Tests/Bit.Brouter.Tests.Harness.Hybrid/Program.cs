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
        Console.Error.WriteLine($"WebView2 Runtime {runtimeVersion}, elevated: {Environment.IsPrivilegedProcess}");

        ApplicationConfiguration.Initialize();
        Application.Run(new HarnessForm(
            // --start-path /items/9 opens the WebView at a deep path, the hybrid counterpart of a deep link.
            startPath: ArgumentValue(args, "--start-path") ?? "/",
            remoteDebuggingPort: ArgumentValue(args, "--remote-debugging-port"),
            userDataFolder: ArgumentValue(args, "--user-data-folder")));
        return 0;
    }

    internal static void Fail(string reason, object? exception)
    {
        Console.Error.WriteLine($"{reason}: {exception}");
        Console.Error.Flush();
        Environment.Exit(1);
    }

    private static string? ArgumentValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
