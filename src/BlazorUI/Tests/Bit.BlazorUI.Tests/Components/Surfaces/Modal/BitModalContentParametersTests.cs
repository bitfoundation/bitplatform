using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalContentParametersTests : BunitTestContext
{
    [TestMethod]
    public void AddShouldNameTheParameterByItsProperty()
    {
        var parameters = new BitModalContentParameters<TestModalContent>
        {
            { c => c.Message, "typed" }
        };

        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual("typed", parameters[nameof(TestModalContent.Message)]);
    }

    [TestMethod]
    public void AddShouldTakeAValueOfTheTypeOfTheParameterOrOneThatConvertsToIt()
    {
        object note = "boxed";

        var parameters = new BitModalContentParameters<TypedContent>
        {
            { c => c.Count, (short)3 },
            { c => c.Note, note },
            { c => c.Data, null },
        };

        Assert.AreEqual(3, parameters[nameof(TypedContent.Count)]);
        Assert.AreEqual("boxed", parameters[nameof(TypedContent.Note)]);
        Assert.IsNull(parameters[nameof(TypedContent.Data)]);
    }

    [TestMethod]
    public void AddShouldRefuseAValueOfAWiderTypeThanTheParameter()
    {
        // The compiler infers the wider type and converts the property to it, so these compile; the renderer
        // would only throw once the modal renders.
        var parameters = new BitModalContentParameters<TypedContent>();
        object notANote = 42;
        int? noCount = null;

        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.Count, 3L));
        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.Note, notANote));
        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.Count, noCount));
        Assert.AreEqual(0, parameters.Count);
    }

    [TestMethod]
    public void AddShouldRefuseWhatIsNotAParameterOfTheComponent()
    {
        var parameters = new BitModalContentParameters<TypedContent>();

        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.NotAParameter, "x"));
        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.Note!.Length, 1));
        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => "constant", "x"));
    }

    [TestMethod]
    public void AddShouldRefuseTheSameParameterTwice()
    {
        var parameters = new BitModalContentParameters<TestModalContent> { { c => c.Message, "first" } };

        Assert.ThrowsExactly<ArgumentException>(() => parameters.Add(c => c.Message, "second"));
    }

    [TestMethod]
    public async Task ShowShouldRenderTheContentWithTheTypedParameters()
    {
        Services.AddSingleton<BitModalService>();

        var container = RenderComponent<BitModalContainer>();

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show<TestModalContent>(new BitModalContentParameters<TestModalContent>
        {
            { c => c.Message, "typed content" }
        });

        container.WaitForAssertion(() => Assert.AreEqual("typed content", container.Find(".test-modal-content").TextContent));
    }

    [TestMethod]
    public async Task TheParametersFactoryShouldTakeTheTypedParameters()
    {
        Services.AddSingleton<BitModalService>();

        var container = RenderComponent<BitModalContainer>();

        var modalService = Services.GetRequiredService<BitModalService>();

        var modalRef = await modalService.Show<TestModalContent>(modal => new BitModalContentParameters<TestModalContent>
        {
            { c => c.Message, modal.Id }
        });

        container.WaitForAssertion(() => Assert.AreEqual(modalRef.Id, container.Find(".test-modal-content").TextContent));
    }



    private class TypedContent : ComponentBase
    {
        [Parameter] public int Count { get; set; }

        [Parameter] public string? Note { get; set; }

        [Parameter] public List<int>? Data { get; set; }

        public string? NotAParameter { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"{Count}{Note}{Data?.Count}");
        }
    }
}
