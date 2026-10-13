using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components;

/// <summary>
/// How a pass of SetParametersAsync hands what its class has not taken down to the next base: through
/// ParametersCache, which every class takes as it starts and sets only right before it calls the base.
/// </summary>
[TestClass]
public class BitComponentBaseSetParametersTests : BunitTestContext
{
    [TestMethod]
    public async Task APassThatStartsWhileAnotherAwaitsAnOnSetHandlerKeepsItsOwnParameters()
    {
        var expanding = new TaskCompletionSource();

        var component = RenderComponent<BitCollapse>(parameters =>
        {
            parameters.Add(p => p.Class, "initial");
        });

        // Expanding a rendered collapse awaits OnExpanding from inside the generated SetParametersAsync, which is
        // what leaves the first pass suspended while the second one runs from start to finish.
        var onExpanding = EventCallback.Factory.Create(this, () => expanding.Task);

        Task first = Task.CompletedTask;
        await component.InvokeAsync(() =>
        {
            first = component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(BitCollapse.OnExpanding), onExpanding },
                { nameof(BitCollapse.Expanded), true },
                { nameof(BitCollapse.Class), "first" },
                { "data-pass", "first" },
            }));
        });

        Assert.IsFalse(first.IsCompleted);

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(BitCollapse.OnExpanding), onExpanding },
            { nameof(BitCollapse.Expanded), true },
            { nameof(BitCollapse.Class), "second" },
            { "data-pass", "second" },
        })));

        await component.InvokeAsync(expanding.SetResult);
        await first;

        var root = component.Find(".bit-col");

        Assert.AreEqual("second", component.Instance.Class);
        Assert.IsTrue(root.ClassList.Contains("second"));
        Assert.IsFalse(root.ClassList.Contains("first"));
        Assert.AreEqual("second", root.GetAttribute("data-pass"));
    }

    [TestMethod]
    public async Task APassThatThrowsLeavesNothingForTheNextOneToApply()
    {
        // A loader has no SetParametersAsync of its own, so its pass starts in the hand-written BitLoadingBase.
        var component = RenderComponent<BitBarsLoading>();

        await Assert.ThrowsExactlyAsync<InvalidCastException>(() => component.InvokeAsync(() =>
            component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(BitBarsLoading.Color), "not a color" },
                { nameof(BitBarsLoading.Class), "stale" },
            }))));

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(BitBarsLoading.Class), "fresh" },
        })));

        Assert.AreEqual("fresh", component.Instance.Class);
        Assert.IsNull(component.Instance.Color);
    }

    [TestMethod]
    public async Task AnInputPassThatThrowsLeavesNothingForTheNextOneToApply()
    {
        var component = RenderComponent<BitTextField>();

        // The text field's own parameters are taken in its generated code, and the rest handed on to the
        // hand-written bases, the first of which to read a wrong value throws.
        await Assert.ThrowsExactlyAsync<InvalidCastException>(() => component.InvokeAsync(() =>
            component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(BitTextField.ReadOnly), "not a bool" },
                { nameof(BitTextField.Class), "stale" },
            }))));

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(BitTextField.Class), "fresh" },
            { "data-pass", "fresh" },
        })));

        var root = component.Find(".bit-tfl");

        Assert.AreEqual("fresh", component.Instance.Class);
        Assert.IsFalse(component.Instance.ReadOnly);
        Assert.AreEqual("fresh", root.GetAttribute("data-pass"));
    }

    [TestMethod]
    public async Task UnmatchedValuesReachTheSplattedAttributesThroughEveryBase()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Label, "Name");
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { { "data-test", "one" } });
        });

        var root = component.Find(".bit-tfl");

        Assert.AreEqual("one", root.GetAttribute("data-test"));
        Assert.IsTrue(component.Instance.ReadOnly);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Label, "Name");
            parameters.Add(p => p.ReadOnly, false);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { { "data-test", "two" } });
        });

        root = component.Find(".bit-tfl");

        Assert.AreEqual("two", root.GetAttribute("data-test"));
        Assert.IsFalse(component.Instance.ReadOnly);
    }
}
