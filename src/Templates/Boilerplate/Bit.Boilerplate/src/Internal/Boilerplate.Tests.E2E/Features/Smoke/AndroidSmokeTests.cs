using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Smoke;

/// <summary>
/// Not parallelized: both apps run on the single connected device/emulator, and launching one backgrounds the other.
/// <see cref="IPlaywrightExtensions"/> lists what a test machine must have connected and installed.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidSmokeTests : SmokeTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();
}
