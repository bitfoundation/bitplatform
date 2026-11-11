namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>
/// A browser test of what dev mode writes into the code. These tests run from their own output, where the Workbench finds no
/// project, so during each test it's pointed at a copy of the pages and the translation files: a change lands in the copy, and no
/// file of the project changes. With an agent it's a Workbench of its own, as the texts it saves stay in its memory.
/// </summary>
[DoNotParallelize]
public abstract class WorkbenchCodeCopyPageTest : WorkbenchPageTest
{
    private static readonly string copyFolder = Path.Combine(AppContext.BaseDirectory, "CodeCopy", "src");

    private protected override Task<WorkbenchTestServer> ServerToTest => HasAgent ? WorkbenchTestsInitializer.CodeCopyServer : base.ServerToTest;

    /// <summary>The project's own src folder, above the tests' output.</summary>
    protected static string ProjectSource
    {
        get
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "Workbench", "Boilerplate.Workbench.csproj")))
                    return folder.FullName;
            }

            throw new InvalidOperationException("The Workbench's project isn't above the tests' output.");
        }
    }

    /// <summary>A file of the copy, by its path under the src folder.</summary>
    protected static string InTheCopy(string relativePath) => Path.Combine(copyFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>A file of the copy, read even while the Workbench is writing it.</summary>
    protected static string ReadTheCopy(string relativePath)
    {
        using var file = new FileStream(InTheCopy(relativePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(file);

        return reader.ReadToEnd();
    }

    /// <summary>Makes this test's Workbench a published copy, which finds no project to read or change.</summary>
    protected static void LeaveTheProject() => WorkbenchFolders.Project = null;

    [TestInitialize]
    public void PointTheWorkbenchAtTheCopy()
    {
        var source = ProjectSource;

        CopyFiles(Path.Combine(source, "Workbench", "Components"), "*.razor");
        CopyFiles(Path.Combine(source, "Workbench", "Resources"), "*.resx");
        CopyFiles(Path.Combine(source, "Shared", "Resources"), "AppStrings*.resx");

        void CopyFiles(string folder, string pattern)
        {
            foreach (var file in Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
            {
                var target = Path.Combine(copyFolder, Path.GetRelativePath(source, file));

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        WorkbenchFolders.Project = Path.Combine(copyFolder, "Workbench");
    }

    [TestCleanup]
    public void PointTheWorkbenchBack() => WorkbenchFolders.Project = null;
}
