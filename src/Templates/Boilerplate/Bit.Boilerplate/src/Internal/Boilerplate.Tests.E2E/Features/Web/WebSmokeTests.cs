using Boilerplate.Tests.E2E.Features.Core;

namespace Boilerplate.Tests.E2E.Features.Web;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebSmokeTests : SmokeTestsBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();
}
