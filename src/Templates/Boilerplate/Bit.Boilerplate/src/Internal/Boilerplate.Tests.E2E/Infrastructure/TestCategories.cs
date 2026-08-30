namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The platform a test drives, so a run can target one of them, e.g. <c>dotnet test --filter "TestCategory=Web"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Tests against the deployed web apps listed in <see cref="DeployedApps"/>.
    /// </summary>
    public const string Web = "Web";

    /// <summary>
    /// Tests driving the installed Android apps' WebView over CDP. See <see cref="HybridAppConnector.LaunchAndroidApp"/>.
    /// </summary>
    public const string Android = "Android";

    /// <summary>
    /// Tests driving the installed Windows apps' WebView2 over CDP. See <see cref="HybridAppConnector.LaunchWindowsApp"/>.
    /// </summary>
    public const string Windows = "Windows";
}
