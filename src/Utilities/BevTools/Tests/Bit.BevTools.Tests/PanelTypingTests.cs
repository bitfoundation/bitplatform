using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// On Blazor Server every keystroke is a round trip. A text box whose value the component writes itself
/// (<c>value="@Query" @oninput="OnQuery"</c>) gets that value written back by every render, over the keys typed
/// meanwhile, so fast typing loses characters: over a 150 ms link the components filter kept "P" of "ProductList".
/// A binding (<c>@bind:get</c>/<c>@bind:set</c>) marks the value as the browser's, and Blazor stops writing it back.
/// </summary>
[TestClass]
public class PanelTypingTests : BunitContext
{
    public PanelTypingTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void Every_text_box_of_the_panel_leaves_its_value_to_the_browser()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        var cut = Render<BevToolsPanel>();
        var seen = new Dictionary<string, string?>();

        var tabs = cut.FindAll(".bev-tab").Select(t => t.TextContent.Trim().Split('\n')[0].Trim()).ToList();
        foreach (var tab in tabs)
        {
            cut.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith(tab, StringComparison.Ordinal)).Click();
            Collect(seen);
        }

        cut.Find("[data-bev-palette-opener]").Click();
        Collect(seen);

        Assert.IsGreaterThanOrEqualTo(9, seen.Count, string.Join(", ", seen.Keys));
        foreach (var (owner, updates) in seen)
        {
            Assert.AreEqual("value", updates, $"{owner}: an oninput text box that is not a binding gets its value written back over typed keys");
        }
    }

    private void Collect(Dictionary<string, string?> seen)
    {
        foreach (var record in Session.Components.All.Where(r => r.IsBevTools && !r.IsDisposed && r.Component is not null && r.Renderer is not null))
        {
            if (BlazorReflection.ResolveState(record.Renderer!, record.Component!) is not { } state || BlazorReflection.CurrentFrames(state) is not { } frames)
            {
                continue;
            }

            foreach (var frame in frames)
            {
                if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "oninput")
                {
                    seen[record.DisplayName] = frame.AttributeEventUpdatesAttributeName;
                }
            }
        }
    }
}
