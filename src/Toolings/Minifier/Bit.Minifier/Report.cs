namespace Bit.Minifier;

/// <summary>
/// What a publish reads of all this: how much smaller the app got, and how to make it smaller still. Written
/// through Exec at high importance, so it shows at the verbosity dotnet publish runs at.
/// </summary>
internal static class Report
{
    private const int Width = 74;
    private const string Indent = "                 ";

    public static void Write(IReadOnlyList<MinifiedAssembly> results, TimeSpan took, bool aggressive)
    {
        var before = results.Sum(r => r.OriginalSize);
        var after = results.Sum(r => r.MinifiedSize);
        var percent = before == 0 ? 0 : (before - after) * 100d / before;
        // box drawing through a console that can't encode it would land as question marks
        var rule = new string(Console.OutputEncoding.CodePage == 65001 ? '─' : '-', Width);

        Console.WriteLine();
        Console.WriteLine(rule);
        Console.WriteLine($"  bit Minifier   freed {Size(before - after)} of {Size(before)} -> {Size(after)} ({percent:N1}% smaller)");
        Console.WriteLine($"{Indent}{results.Count} {(results.Count == 1 ? "assembly" : "assemblies")} in {took.TotalSeconds:N1}s");
        if (aggressive is false)
        {
            Console.WriteLine($"{Indent}Go smaller: <BitMinifyAggressive>true</BitMinifyAggressive>");
        }
        Console.WriteLine(rule);
        Console.WriteLine();
    }

    private static string Size(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024d):N2} MB" : $"{bytes / 1024d:N0} KB";
}
