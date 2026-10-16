using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

/// <summary>
/// What a text field sends before the page is interactive: the after-render pass that wires every JavaScript feature
/// of the field never runs on a prerender, and the renderer it is handed throws on every JS call, so a field with all
/// of them turned on still has to come out of the render alone - value, label and counter included.
/// </summary>
[TestClass]
public class BitTextFieldPrerenderTests
{
    [TestMethod]
    public async Task BitTextFieldShouldRenderItsValueAndChromeBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitTextField>(new Dictionary<string, object?>
        {
            [nameof(BitTextField.Label)] = "Email",
            [nameof(BitTextField.Value)] = "ada@example.com",
            [nameof(BitTextField.Name)] = "email",
            [nameof(BitTextField.ShowCount)] = true,
            [nameof(BitTextField.MaxLength)] = 32,
            [nameof(BitTextField.ShowClearButton)] = true,
            [nameof(BitTextField.ErrorMessage)] = "Taken.",
        });

        StringAssert.Contains(html, "bit-tfl-inp");
        StringAssert.Contains(html, "value=\"ada@example.com\"");
        StringAssert.Contains(html, "name=\"email\"");
        StringAssert.Contains(html, "Email");
        StringAssert.Contains(html, "15/32");
        StringAssert.Contains(html, "bit-tfl-cbt");
        StringAssert.Contains(html, "Taken.");
    }

    [TestMethod]
    public async Task BitTextFieldWithEveryScriptFeatureShouldStillRenderBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitTextField>(new Dictionary<string, object?>
        {
            [nameof(BitTextField.Multiline)] = true,
            [nameof(BitTextField.AutoHeight)] = true,
            [nameof(BitTextField.PreventEnter)] = true,
            [nameof(BitTextField.SelectOnFocus)] = true,
            [nameof(BitTextField.Immediate)] = true,
            [nameof(BitTextField.ClearOnEscape)] = true,
            [nameof(BitTextField.GhostText)] = "suggestion",
            [nameof(BitTextField.Prefix)] = "https://",
            [nameof(BitTextField.AutoFocus)] = true,
            [nameof(BitTextField.Value)] = "hello",
        });

        StringAssert.Contains(html, "<textarea");
        StringAssert.Contains(html, "bit-tfl-mla");
        StringAssert.Contains(html, "bit-tfl-gho");
        StringAssert.Contains(html, "https://");
    }
}
