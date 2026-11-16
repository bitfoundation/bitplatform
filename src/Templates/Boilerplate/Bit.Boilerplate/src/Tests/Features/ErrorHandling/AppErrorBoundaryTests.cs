using Bunit;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// What <c>AppErrorBoundary</c> catches is shown on the boundary's own page, which takes the place of the layout and so of
/// its modal and snack bar containers: a message box asked for then has nowhere to render, and only logs that no modal
/// container is mounted. A known exception's message is written for the user, so it goes under the page's title.
/// </summary>
[TestClass, TestCategory("UITest")]
public class AppErrorBoundaryTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AKnownException_Should_ShowItsMessageOnTheBoundarysPage_RatherThanInAMessageBox()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var shownModals = 0;
        ctx.Services.GetRequiredService<BitModalService>().OnAddModal += _ =>
        {
            Interlocked.Increment(ref shownModals);
            return Task.CompletedTask;
        };

        var cut = ctx.Render<AppErrorBoundary>(parameters => parameters.AddChildContent<KnownExceptionThrower>());

        await cut.WaitForAssertionAsync(() => Assert.AreEqual(KnownExceptionThrower.Message, cut.Find(".bit-erb-msg").TextContent.Trim(),
            "The known exception's message goes under the boundary's title."), TimeSpan.FromSeconds(10));

        Assert.AreEqual(0, Volatile.Read(ref shownModals), "The boundary's page took the place of the modal container, so no modal is asked for.");
    }

    private sealed class KnownExceptionThrower : ComponentBase
    {
        public const string Message = "The order has already been shipped.";

        protected override void OnInitialized() => throw new DomainLogicException(Message);
    }
}
