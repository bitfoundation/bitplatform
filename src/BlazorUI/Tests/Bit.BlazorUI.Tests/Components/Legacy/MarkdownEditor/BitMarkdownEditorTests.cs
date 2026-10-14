using System.Threading.Tasks;
using Bit.BlazorUI.Tests;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.MarkdownEditor;

[TestClass]
public class BitMarkdownEditorTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectDisabled(bool isEnabled)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
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
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Id, "id");
            parameters.Add(p => p.DefaultValue, "hello");
        });

        // The editor is set up under the root's id, on the default value, and the textarea already carries it.
        var init = Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.init");
        Assert.AreEqual("id", init.Arguments[0]);
        Assert.AreEqual("hello", init.Arguments[3]);
        Assert.AreEqual("hello", component.Find("textarea").GetAttribute("value"));

        // Without a bound value or an OnChange there is nothing for the script to call back, so no reference is handed over.
        Assert.IsNull(init.Arguments[2]);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldHandTheScriptACallbackWhenAChangeHasSomewhereToGo()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        RenderComponent<BitMarkdownEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string? _) => { });
        });

        Assert.IsNotNull(Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.init").Arguments[2]);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSplatTheHtmlAttributesOnTheRoot()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMarkdownEditorLegacy>(0);
            builder.AddAttribute(1, nameof(BitMarkdownEditorLegacy.Id), "editor");
            builder.AddAttribute(2, nameof(BitMarkdownEditorLegacy.Class), "custom-class");
            builder.AddAttribute(3, nameof(BitMarkdownEditorLegacy.Style), "padding:1px");
            builder.AddAttribute(4, nameof(BitMarkdownEditorLegacy.Dir), BitDir.Rtl);
            builder.AddAttribute(5, "data-test", "root");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-mde");

        Assert.AreEqual("editor", root.Id);
        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:1px");
        Assert.IsNull(component.Find("textarea").GetAttribute("data-test"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldInvokeOnChange()
    {
        string? changed = null;

        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters =>
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
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters =>
        {
            parameters.Add(p => p.Value, "initial");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.setValue", 1);

        Assert.AreEqual("initial", component.Instance.Value);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "updated");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.setValue", 2);

        Assert.AreEqual("updated", component.Instance.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldRunCommand()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.run");

        var component = RenderComponent<BitMarkdownEditorLegacy>();

        await component.Instance.Run(BitMarkdownEditorLegacyCommand.Bold);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.run");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldAddContent()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.add");

        var component = RenderComponent<BitMarkdownEditorLegacy>();

        await component.Instance.Add("block", BitMarkdownEditorLegacyContentType.Block);
        await component.Instance.Add("inline", BitMarkdownEditorLegacyContentType.Inline);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.add", 2);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldDisposeJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.init");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.MarkdownEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue");

        var component = RenderComponent<BitMarkdownEditorLegacy>(parameters => parameters.Add(p => p.Id, "editor"));

        await component.Instance.DisposeAsync();

        Assert.AreEqual("editor", Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.dispose").Arguments[0]);

        // Disposing again is a no-op rather than a second teardown.
        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.MarkdownEditor.dispose", 1);
    }
}
