using System.Globalization;
using Bit.Bmage.Text.Unicode;

namespace Bit.Bmage.Tests;

/// <summary>
/// The bidirectional algorithm against Unicode's own conformance data: a committed sample of BidiTest.txt and
/// BidiCharacterTest.txt, or the whole of both when <c>BMAGE_UCD_DIR</c> names a directory holding them.
/// </summary>
[TestClass]
public sealed class BidiConformanceTests
{
    private static readonly char[] Separators = [' ', '\t'];

    private static string Source(string name)
    {
        var directory = Environment.GetEnvironmentVariable("BMAGE_UCD_DIR");
        return directory is { Length: > 0 } && File.Exists(Path.Combine(directory, name + ".txt"))
            ? Path.Combine(directory, name + ".txt")
            : Path.Combine("Assets", name + ".sample.txt");
    }

    [TestMethod]
    public void PassesBidiCharacterTest()
    {
        var failures = new List<string>();
        var count = 0;
        foreach (var line in File.ReadLines(Source("BidiCharacterTest")))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var fields = line.Split(';');
            var codePoints = fields[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h, NumberStyles.HexNumber)).ToArray();
            var direction = int.Parse(fields[1], CultureInfo.InvariantCulture) switch { 0 => 0, 1 => 1, _ => -1 };
            var expectedParagraph = int.Parse(fields[2], CultureInfo.InvariantCulture);
            var expectedLevels = fields[3].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var expectedOrder = fields[4].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            var types = codePoints.Select(UnicodeData.GetBidiClass).ToArray();

            var (paragraph, levels, order) = Run(types, codePoints, direction);
            count++;
            var actualLevels = string.Join(' ', levels.Select((l, i) => BidiParagraph.IsRemovedByX9(types[i]) ? "x" : l.ToString(CultureInfo.InvariantCulture)));
            if (paragraph != expectedParagraph || actualLevels != string.Join(' ', expectedLevels) || !order.SequenceEqual(expectedOrder))
                failures.Add($"{fields[0]}; dir {fields[1]}: paragraph {paragraph}/{expectedParagraph}, levels [{actualLevels}] / [{fields[3]}], order [{string.Join(' ', order)}] / [{fields[4]}]");
        }
        Assert.IsGreaterThan(1000, count);
        Assert.IsEmpty(failures, $"{failures.Count} of {count} failed:\n" + string.Join('\n', failures.Take(20)));
    }

    [TestMethod]
    public void PassesBidiTest()
    {
        var failures = new List<string>();
        var count = 0;
        string[] levels = [];
        int[] reorder = [];
        foreach (var line in File.ReadLines(Source("BidiTest")))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            if (line.StartsWith("@Levels:", StringComparison.Ordinal))
            {
                levels = line["@Levels:".Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                continue;
            }
            if (line.StartsWith("@Reorder:", StringComparison.Ordinal))
            {
                reorder = line["@Reorder:".Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                continue;
            }
            if (line[0] == '@') continue;
            var fields = line.Split(';');
            var types = fields[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<BidiClass>).ToArray();
            var bits = int.Parse(fields[1].Trim(), CultureInfo.InvariantCulture);
            foreach (var (bit, direction) in new[] { (1, -1), (2, 0), (4, 1) })
            {
                if ((bits & bit) == 0) continue;
                var (_, actual, order) = Run(types, null, direction);
                count++;
                var actualLevels = string.Join(' ', actual.Select((l, i) => BidiParagraph.IsRemovedByX9(types[i]) ? "x" : l.ToString(CultureInfo.InvariantCulture)));
                if (actualLevels != string.Join(' ', levels) || !order.SequenceEqual(reorder))
                    failures.Add($"{fields[0]}; {(direction < 0 ? "auto" : direction == 0 ? "LTR" : "RTL")}: levels [{actualLevels}] / [{string.Join(' ', levels)}], order [{string.Join(' ', order)}] / [{string.Join(' ', reorder)}]");
            }
        }
        Assert.IsGreaterThan(1000, count);
        Assert.IsEmpty(failures, $"{failures.Count} of {count} failed:\n" + string.Join('\n', failures.Take(20)));
    }

    /// <summary>
    /// Resolves the input as the test files mean it: each paragraph separator ends a paragraph, and the whole input is
    /// one line. The visual order leaves out the characters X9 removes.
    /// </summary>
    private static (int Paragraph, byte[] Levels, int[] Order) Run(BidiClass[] types, int[]? codePoints, int direction)
    {
        var levels = new byte[types.Length];
        var order = new List<int>();
        var firstParagraph = -1;
        for (var start = 0; start < types.Length;)
        {
            var end = start;
            while (end < types.Length && types[end] != BidiClass.B) end++;
            if (end < types.Length) end++;
            var paragraph = new BidiParagraph(types[start..end], codePoints?[start..end], direction);
            if (firstParagraph < 0) firstParagraph = paragraph.ParagraphLevel;
            var lineLevels = paragraph.GetLineLevels(0, end - start);
            lineLevels.CopyTo(levels, start);

            var kept = Enumerable.Range(start, end - start).Where(i => !BidiParagraph.IsRemovedByX9(types[i])).ToArray();
            var keptLevels = kept.Select(i => levels[i]).ToArray();
            order.AddRange(BidiParagraph.GetVisualOrder(keptLevels).Select(i => kept[i]));
            start = end;
        }
        return (firstParagraph < 0 ? 0 : firstParagraph, levels, [.. order]);
    }
}
