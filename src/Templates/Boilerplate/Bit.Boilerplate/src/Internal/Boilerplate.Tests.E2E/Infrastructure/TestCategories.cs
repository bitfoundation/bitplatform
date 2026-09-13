namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The platform a test drives, so a run can target one of them, e.g. <c>dotnet test --filter "TestCategory=Web"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>Tests against the deployed web apps listed in <see cref="DeployedApps"/>.</summary>
    public const string Web = "Web";

    /// <summary>Tests driving the installed apps' WebView over CDP; see <see cref="IPlaywrightExtensions"/>.</summary>
    public const string Android = "Android";
    public const string Windows = "Windows";

    /// <summary>Browserless tests calling the deployed APIs and their database through <see cref="DeployedApiClientProvider"/>.</summary>
    public const string Api = "Api";
}
