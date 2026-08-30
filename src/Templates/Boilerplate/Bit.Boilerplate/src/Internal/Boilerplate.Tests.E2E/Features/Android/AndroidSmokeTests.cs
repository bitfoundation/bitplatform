using Boilerplate.Tests.E2E.Features.Core;

namespace Boilerplate.Tests.E2E.Features.Android;

/// <summary>
/// Not parallelized: both apps run on the single connected device/emulator, and launching one backgrounds the other.
/// See <see cref="HybridAppConnector"/> for what a test machine must have connected and installed.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidSmokeTests : SmokeTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();
}
