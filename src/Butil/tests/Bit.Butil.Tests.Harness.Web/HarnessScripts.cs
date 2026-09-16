namespace ButilTests.Harness.Web;

/// <summary>
/// Bit.Butil's script-loading mode for the whole host, by the names the suites pass as
/// <c>--ButilHarness:Scripts=&lt;name&gt;</c>: <c>bundle</c> (the default) or <c>lazy</c>.
/// </summary>
/// <remarks>
/// Host-wide rather than per request because the library's own toggle is process-wide: a Server circuit and the
/// prerender pass read <c>BitButil.LazyScriptsEnabled</c> from the same static, so one process cannot serve both.
/// </remarks>
public static class HarnessScripts
{
    public const string ConfigurationKey = "ButilHarness:Scripts";

    public const string Bundle = "bundle";
    public const string Lazy = "lazy";

    public static bool IsLazy(IConfiguration configuration) => Resolve(configuration[ConfigurationKey]) is Lazy;

    public static string Resolve(string? scripts) => scripts switch
    {
        null or "" or Bundle => Bundle,
        Lazy => Lazy,
        _ => throw new InvalidOperationException($"Unknown {ConfigurationKey} '{scripts}'. Expected {Bundle} or {Lazy}.")
    };
}
