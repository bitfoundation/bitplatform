namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Where the Workbench keeps its files on disk.</summary>
public static class WorkbenchFolders
{
    /// <summary>The folder of the Workbench's project file, found above the build output; null in a published copy. Tests point it at a copy of the project.</summary>
    public static string? Project { get; set; } = FindProject();

    /// <summary>The src folder that holds the Workbench's project and the app's; null in a published copy.</summary>
    public static string? Source => Project is { } folder ? Path.GetDirectoryName(folder) : null;

    /// <summary>The review document and the page states saved with it, in a folder Git ignores.</summary>
    public static string Reviews => Path.Combine(Project ?? AppContext.BaseDirectory, "App_Data", "Reviews");

    private static string? FindProject()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Boilerplate.Workbench.csproj")))
                return folder.FullName;
        }

        return null;
    }
}
