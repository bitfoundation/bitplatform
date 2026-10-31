namespace Bit.Bswup.Tests.Mcp.TestInfra;

/// <summary>
/// Service-worker files shaped like the ones people actually write - the correct one, and one of
/// each way of getting it wrong that the inspector exists to catch.
/// </summary>
public static class ServiceWorkerFixtures
{
    public const string Import = "self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');";

    /// <summary>A file with nothing wrong with it: settings first, the engine import last.</summary>
    public const string Clean = $$"""
        // A perfectly ordinary worker.
        self.assetsExclude = [/\.scp\.css$/];
        self.caseInsensitiveUrl = true;
        self.isPassive = false;

        {{Import}}
        """;

    /// <summary>The classic failure: the setting is assigned where the engine can no longer read it.</summary>
    public const string SettingAfterImport = $"""
        self.isPassive = false;

        {Import}

        self.caseInsensitiveUrl = true;
        """;

    /// <summary>A name the shipped worker does not read - set, then silently ignored.</summary>
    public const string UnknownSetting = $"""
        self.assetsExclud = [/\.scp\.css$/];

        {Import}
        """;

    /// <summary>The cleanup worker, which makes every other setting in the file irrelevant.</summary>
    public const string Cleanup = """
        self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');
        """;

    /// <summary>
    /// <c>self.a = self.a = ... 1;</c>, 7,000 links deep. Every link's value runs to the end of the
    /// chain, so the values add up far past what the scanner reads (256,000 characters) while the
    /// file itself, at 63,002 characters, stays under the tools' 64,000-character script limit: the
    /// one shape that reaches the second limit rather than the first. The tests that refuse it
    /// assert the "chained too deeply" reason, so a limit change that let the first one fire instead
    /// fails them rather than passing for the wrong reason.
    /// </summary>
    public static readonly string ChainedAssignments = string.Concat(Enumerable.Repeat("self.a = ", 7_000)) + "1;";

    /// <summary>Wraps a body in a file that imports the engine after it, the way a real one is written.</summary>
    public static string WithImport(string body) => $"{body}\n\n{Import}\n";
}
