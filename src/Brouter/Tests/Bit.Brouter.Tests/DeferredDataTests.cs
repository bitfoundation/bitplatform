using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class DeferredDataTests : BunitTestContext
{
    [TestMethod]
    public void Route_reveals_immediately_while_deferred_data_is_pending()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/post");

        var cut = RenderComponent<DeferredHost>();

        cut.WaitForAssertion(() =>
        {
            // Critical content is on screen although the deferred task hasn't resolved.
            Assert.IsNotNull(cut.Find("[data-testid=post]"));
            Assert.IsNotNull(cut.Find("[data-testid=comments-pending]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=comments]").Count);
        });
    }

    [TestMethod]
    public void Deferred_data_streams_in_when_the_task_resolves()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/post");

        var cut = RenderComponent<DeferredHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=comments-pending]"));

        cut.Instance.SlowGate.SetResult("42 comments");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("42 comments", cut.Find("[data-testid=comments]").TextContent);
            Assert.AreEqual(0, cut.FindAll("[data-testid=comments-pending]").Count);
        });
    }

    [TestMethod]
    public void Deferred_failure_renders_the_error_fragment()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/post");

        var cut = RenderComponent<DeferredHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=comments-pending]"));

        cut.Instance.SlowGate.SetException(new InvalidOperationException("comments api down"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Find("[data-testid=comments-error]").TextContent.Contains("comments api down"));
            Assert.IsNotNull(cut.Find("[data-testid=post]")); // the page itself is unaffected
        });
    }
}
