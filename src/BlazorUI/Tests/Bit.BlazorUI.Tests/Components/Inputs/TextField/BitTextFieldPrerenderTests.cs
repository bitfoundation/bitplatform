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

        Assert.Contains("bit-tfl-inp", html);
        Assert.Contains("value=\"ada@example.com\"", html);
        Assert.Contains("name=\"email\"", html);
        Assert.Contains("Email", html);
        Assert.Contains("15/32", html);
        Assert.Contains("bit-tfl-cbt", html);
        Assert.Contains("Taken.", html);
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

        Assert.Contains("<textarea", html);
        Assert.Contains("bit-tfl-mla", html);
        Assert.Contains("bit-tfl-gho", html);
        Assert.Contains("https://", html);
    }
}
