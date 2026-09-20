using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Smoke;

/// <summary>
/// Not parallelized: every Client.Windows app answers on the same hard-coded CDP port 9222, so two sessions at once
/// would attach to whichever app won it. <see cref="IPlaywrightExtensions"/> lists what a test machine must have installed.
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsSmokeTests : SmokeTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();
}
