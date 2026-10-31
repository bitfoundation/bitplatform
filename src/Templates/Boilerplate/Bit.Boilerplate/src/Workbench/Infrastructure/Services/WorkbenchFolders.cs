namespace Boilerplate.Workbench.Infrastructure.Services;

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

    public static string? Project => project.Value;

    public static string Reviews => Path.Combine(Project ?? AppContext.BaseDirectory, "App_Data", "Reviews");
}
