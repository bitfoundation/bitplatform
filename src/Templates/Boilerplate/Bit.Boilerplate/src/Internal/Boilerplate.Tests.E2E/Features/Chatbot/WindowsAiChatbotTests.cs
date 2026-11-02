using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// Not parallelized: every session starts its Client.Windows app on the same CDP port 9222, so two sessions at once
/// would attach to whichever app won it.
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsAiChatbotTests : AiChatbotTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();
}
