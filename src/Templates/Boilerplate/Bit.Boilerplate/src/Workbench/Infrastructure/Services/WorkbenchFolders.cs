namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Where the Workbench keeps its files on disk.</summary>
public static class WorkbenchFolders
{
    private static readonly Lazy<string?> project = new(() =>
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Boilerplate.Workbench.csproj")))
                return folder.FullName;
        }

        return null;
    });

    /// <summary>The folder of the Workbench's project file, found above the build output; null in a published copy.</summary>
    public static string? Project => project.Value;

    /// <summary>The review document and the page states saved with it, in a folder Git ignores.</summary>
    public static string Reviews => Path.Combine(Project ?? AppContext.BaseDirectory, "App_Data", "Reviews");
}
