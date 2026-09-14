namespace Bit.Brouter.Tests.Harness.Hybrid;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new HarnessForm(StartPathFrom(args)));
    }

    // --start-path /items/9 opens the WebView at a deep path, the hybrid counterpart of a deep link.
    private static string StartPathFrom(string[] args)
    {
        var index = Array.IndexOf(args, "--start-path");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : "/";
    }
}
