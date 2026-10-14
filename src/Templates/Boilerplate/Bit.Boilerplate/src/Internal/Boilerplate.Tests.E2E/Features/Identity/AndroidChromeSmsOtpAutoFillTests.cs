using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidChromeSmsOtpAutoFillTests : SmsOtpAutoFillTestsBase
{
    protected override IAppOpener AppOpener => new AndroidChromeAppOpener();
}
