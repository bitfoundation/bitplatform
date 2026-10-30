using System.Globalization;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless.Tests.Testing;

/// <summary>The bUnit helpers of Bit.Bheadless.Testing: keyboard events, lookups, browser emulation and the announcer.</summary>
[TestClass]
public class TestingHelpersTests : BitBhTestContext
{
    private readonly List<KeyboardEventArgs> _keys = [];

    private IElement KeyTarget()
        => Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "tabindex", "0");
            builder.AddAttribute(2, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, _keys.Add));
            builder.CloseElement();
        }).Find("div");

    [TestMethod]
    public void Readme_sample_runs()
    {
        var cut = Render<BitBhSwitch>(p => p.AddUnmatched("aria-label", "Wi-Fi"));
        var toggle = cut.Find("[role=switch]");

        Assert.AreEqual("Wi-Fi", toggle.AccessibleName());
        toggle.Click();
        Assert.AreEqual("true", cut.Find("[role=switch]").GetAttribute("aria-checked"));
        cut.ShouldBeAccessible();
    }

    [TestMethod]
    public void Press_sets_the_code_of_a_us_keyboard()
    {
        KeyTarget().PressSequence("a", "Z", "7", " ", "/", "Enter", "ArrowDown", "PageUp", "F5", "é", "Dead");
        Assert.AreSequenceEqual(
            new[] { "KeyA", "KeyZ", "Digit7", "Space", "Slash", "Enter", "ArrowDown", "PageUp", "F5", "", "" },
            _keys.Select(k => k.Code).ToArray());
    }

    [TestMethod]
    public void Type_holds_shift_for_uppercase_letters_and_shifted_symbols()
    {
        KeyTarget().Type("aB?1!");
        Assert.AreSequenceEqual(new[] { "a", "B", "?", "1", "!" }, _keys.Select(k => k.Key).ToArray());
        Assert.AreSequenceEqual(new[] { "KeyA", "KeyB", "Slash", "Digit1", "Digit1" }, _keys.Select(k => k.Code).ToArray());
        Assert.AreSequenceEqual(new[] { false, true, true, false, true }, _keys.Select(k => k.ShiftKey).ToArray());
    }

    [TestMethod]
    public void Active_descendant_lookup_accepts_any_id()
    {
        var cut = Render(builder => builder.AddMarkupContent(0,
            "<div role=\"listbox\" aria-label=\"Fruit\" tabindex=\"0\" aria-activedescendant=\"o'1&quot;\">" +
            "<div role=\"option\" id=\"o'1&quot;\" aria-selected=\"true\">Apple</div></div>"));
        Assert.AreEqual("Apple", cut.ActiveDescendant(cut.Find("[role=listbox]"))!.TextContent);
    }

    [TestMethod]
    public async Task Collection_order_query_before_any_render_completes_with_null()
    {
        var order = JSInterop.JSRuntime.InvokeAsync<string[][]?>("collection.order", "items").AsTask();
        Assert.AreSame(order, await Task.WhenAny(order, Task.Delay(TimeSpan.FromSeconds(5))));
        Assert.IsNull(await order);
    }

    [TestMethod]
    public async Task Announcer_timeout_message_is_culture_invariant()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var error = await Assert.ThrowsExactlyAsync<TimeoutException>(() => Announcer.WaitForAsync("never", TimeSpan.FromMilliseconds(200)));
            Assert.Contains("within 0.2 s", error.Message);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
