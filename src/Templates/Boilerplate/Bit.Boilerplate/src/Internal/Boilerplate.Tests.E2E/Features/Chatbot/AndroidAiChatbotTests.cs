using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// Not parallelized: both apps run on the single connected device/emulator, and launching one backgrounds the other.
/// Sales has no Android build, so its rows report inconclusive.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidAiChatbotTests : AiChatbotTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();
}
