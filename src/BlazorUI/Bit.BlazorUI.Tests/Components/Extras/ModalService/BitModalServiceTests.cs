using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ModalService;

[TestClass]
public class BitModalServiceTests : BunitTestContext
{
    private BitModalService ModalService => Services.GetRequiredService<BitModalService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderModalInContainer()
    {
        var message = "Hello modal";

        var container = RenderComponent<BitModalContainer>();

        await ModalService.Show<TestModalContent>(new Dictionary<string, object>
        {
            { nameof(TestModalContent.Message), message }
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
            Assert.IsTrue(container.Markup.Contains(message));
        });
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        });

        modalRef.Close();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderPersistentModalAfterContainerInit()
    {
        var modalRef = await ModalService.Show<TestModalContent>(persistent: true);

        var container = RenderComponent<BitModalContainer>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        });

        Assert.IsTrue(modalRef.Persistent);
    }
}
