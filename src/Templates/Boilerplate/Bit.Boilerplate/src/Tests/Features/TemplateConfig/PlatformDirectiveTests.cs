//-:cnd:noEmit
namespace Boilerplate.Tests.Features.TemplateConfig;

[TestClass, TestCategory("UnitTest")]
public class PlatformDirectiveTests
{
    private static readonly Regex platformDirective = new(@"^\s*#(if|elif)\b.*\b(Android|iOS|Windows|Mac)\b", RegexOptions.Compiled);

    [TestMethod]
    public void EveryPlatformDirective_Should_BeHiddenFromTheTemplateEngine()
    {
        var templateRoot = FindTemplateRoot();

        var separator = Path.DirectorySeparatorChar;

        var exposed = Directory.EnumerateFiles(Path.Combine(templateRoot, "src"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
            .Where(path => path.Contains($"{separator}bin{separator}") is false && path.Contains($"{separator}obj{separator}") is false)
            .SelectMany(path => ExposedPlatformDirectives(templateRoot, path))
            .ToArray();

        Assert.IsEmpty(exposed,
            $"These platform directives are read by the template engine, which can drop them from a generated project, " +
            $"and with them the platform's code. Put each between //-:cnd:noEmit and //+:cnd:noEmit: {string.Join(", ", exposed)}");
    }

    private static IEnumerable<string> ExposedPlatformDirectives(string templateRoot, string path)
    {
        var processed = true;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;

            if (line.Contains("-:cnd", StringComparison.Ordinal))
            {
                processed = false;
            }
            else if (line.Contains("+:cnd", StringComparison.Ordinal))
            {
                processed = true;
            }
            else if (processed && platformDirective.IsMatch(line))
            {
                yield return $"{Path.GetRelativePath(templateRoot, path)}:{lineNumber}";
            }
        }
    }

    private static string FindTemplateRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
        }

        return directory!.FullName;
    }
}
