namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// One Workbench is the desk of every chat on a project, whichever worktree it runs from, so the review and the agents' tasks
/// live in the user's folder for the clone, the same for all its worktrees. Not in parallel: the project folder is one for the
/// whole process.
/// </summary>
[TestClass, TestCategory("UnitTest"), DoNotParallelize]
public class WorkbenchFoldersTests
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"wb-folders-{Guid.NewGuid():N}");
    private string? projectBefore;
    private int? portBefore;

    [TestInitialize]
    public void Initialize() => (projectBefore, portBefore) = (WorkbenchFolders.Project, WorkbenchFolders.Port);

    [TestCleanup]
    public void Cleanup()
    {
        (WorkbenchFolders.Project, WorkbenchFolders.Port) = (projectBefore, portBefore);

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string Folder(params string[] parts)
    {
        var folder = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string UserData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bit-workbench");

    [TestMethod]
    public void EveryWorktreeOfAClone_Should_ShareOneReviewOutsideTheRepository()
    {
        var gitDir = Folder("MyApp", ".git");
        File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/develop\n");

        var linkedGitDir = Folder("MyApp", ".git", "worktrees", "deploy");
        File.WriteAllText(Path.Combine(linkedGitDir, "commondir"), "../..\n");
        File.WriteAllText(Path.Combine(linkedGitDir, "HEAD"), "ref: refs/heads/feature/deploy\n");
        Folder("MyApp-deploy");
        File.WriteAllText(Path.Combine(root, "MyApp-deploy", ".git"), $"gitdir: {linkedGitDir}\n");

        WorkbenchFolders.Project = Folder("MyApp", "src", "Workbench");

        var main = WorkbenchFolders.Reviews;

        Assert.AreEqual(Path.Combine(root, "MyApp"), WorkbenchFolders.WorkingTree);
        Assert.AreEqual("develop", WorkbenchFolders.Branch);
        Assert.StartsWith(Path.Combine(UserData, "MyApp-"), main);
        Assert.AreEqual("Reviews", Path.GetFileName(main));

        WorkbenchFolders.Project = Folder("MyApp-deploy", "src", "Workbench");

        Assert.AreEqual(main, WorkbenchFolders.Reviews, "The linked worktree shares the main one's review.");
        Assert.AreEqual(Path.Combine(root, "MyApp-deploy"), WorkbenchFolders.WorkingTree);
        Assert.AreEqual("feature/deploy", WorkbenchFolders.Branch);
    }

    [TestMethod]
    public void TwoClones_Should_KeepTheirReviewsApart()
    {
        Folder("One", "MyApp", ".git");
        Folder("Two", "MyApp", ".git");

        WorkbenchFolders.Project = Folder("One", "MyApp", "src", "Workbench");
        var one = WorkbenchFolders.Reviews;

        WorkbenchFolders.Project = Folder("Two", "MyApp", "src", "Workbench");

        Assert.AreNotEqual(one, WorkbenchFolders.Reviews);
    }

    [TestMethod]
    public void TwoProjectsInOneClone_Should_KeepTheirReviewsApart()
    {
        Folder("Apps", ".git");

        WorkbenchFolders.Project = Folder("Apps", "Shop", "src", "Workbench");
        var shop = WorkbenchFolders.Reviews;

        WorkbenchFolders.Project = Folder("Apps", "Crm", "src", "Workbench");

        Assert.AreNotEqual(shop, WorkbenchFolders.Reviews);
        Assert.StartsWith(Path.Combine(UserData, "Apps-"), WorkbenchFolders.Reviews);
    }

    [TestMethod]
    public void AWorkbenchOnAnotherPort_Should_KeepAReviewOfItsOwn()
    {
        Folder("MyApp", ".git");
        File.WriteAllText(Path.Combine(Folder("MyApp", "src", "Workbench", "Properties"), "launchSettings.json"), "{ \"profiles\": { \"MyApp\": { \"applicationUrl\": \"http://localhost:6400\" } } }");

        WorkbenchFolders.Project = Folder("MyApp", "src", "Workbench");
        WorkbenchFolders.Port = WorkbenchFolders.PortOf("http://localhost:6400");

        var shared = WorkbenchFolders.Reviews;

        WorkbenchFolders.Port = WorkbenchFolders.PortOf("https://localhost:6401/;http://localhost:6402");

        Assert.AreEqual("Reviews", Path.GetFileName(shared));
        Assert.AreEqual(Path.Combine(Path.GetDirectoryName(shared)!, "Reviews-6401"), WorkbenchFolders.Reviews, "Two processes never share a review.");
    }

    [TestMethod]
    public void AProjectOutsideGit_Should_KeepItsReviewNextToIt()
    {
        WorkbenchFolders.Project = Folder("Plain", "src", "Workbench");

        Assert.AreEqual(Path.Combine(root, "Plain", "src", "Workbench", "App_Data", "Reviews"), WorkbenchFolders.Reviews);
        Assert.IsNull(WorkbenchFolders.WorkingTree);
        Assert.IsNull(WorkbenchFolders.Branch);
    }
}
