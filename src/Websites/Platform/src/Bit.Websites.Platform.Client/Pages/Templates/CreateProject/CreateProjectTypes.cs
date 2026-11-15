namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public enum CreateProjectGoal
{
    Real,
    Try
}

public enum CreateProjectStart
{
    Blank,
    Admin,
    Store
}

public enum CreateProjectDatabase
{
    PostgreSql,
    Sqlite,
    SqlServer,
    MySql
}

public enum CreateProjectErrors
{
    OwnServer,
    Sentry,
    AppInsights
}

public enum CreateProjectComputer
{
    Windows,
    MacOs,
    Linux
}

public enum CreateProjectDevice
{
    Android,
    Ios,
    Windows,
    MacOs
}

public enum CreateProjectHelpTopic
{
    General,
    Name,
    Goal,
    Start,
    Customers,
    Features,
    Errors,
    Devices,
    Database,
    GitHub,
    Offline,
    Line
}

public sealed record CreateProjectChoice<TValue>(TValue Value, string Text, string? Description = null, string? Badge = null);

public static class CreateProjectNames
{
    public static string Of(CreateProjectComputer computer) => computer switch
    {
        CreateProjectComputer.MacOs => "Mac",
        CreateProjectComputer.Linux => "Linux",
        _ => "Windows"
    };

    public static string Of(CreateProjectDevice device) => device switch
    {
        CreateProjectDevice.Android => "Android",
        CreateProjectDevice.Ios => "iPhone and iPad",
        CreateProjectDevice.Windows => "Windows app",
        _ => "Mac app"
    };

    public static string ListOf(IEnumerable<CreateProjectDevice> devices) => string.Join(", ", devices.Select(Of));
}
