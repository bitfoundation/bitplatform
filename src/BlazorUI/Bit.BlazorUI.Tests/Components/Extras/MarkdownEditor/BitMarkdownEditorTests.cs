using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

[TestClass]
public class BitMarkdownEditorTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectIsEnabled(bool isEnabled)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-mde");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldInitializeWithDefaultValue()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "hello");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.init");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldInvokeOnChange()
    {
        string? changed = null;

        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create(this, (string? value) =>
            {
                changed = value;
                return Task.CompletedTask;
            }));
        });

        await component.Instance._OnChange("new value");

        Assert.AreEqual("new value", changed);
        Assert.AreEqual("new value", component.Instance.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldSetValueAndCallJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Value, "initial");
        });

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Value, "updated");
        });

        await component.InvokeAsync(() => component.Instance._OnChange("updated"));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.setValue", 2);

        Assert.AreEqual("updated", component.Instance.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldRunCommand()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.run");

        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.Run(BitMarkdownEditorCommand.Bold);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.run");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldAddContent()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.add");

        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.Add("block", BitMarkdownEditorContentType.Block);
        await component.Instance.Add("inline", BitMarkdownEditorContentType.Inline);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.add", 2);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldDisposeJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.dispose");
    }
}
