using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// The behaviour of the newer components that the browser runtime carries and bUnit cannot see: typed characters
/// refused before they reach an input, a selection made on focus, the wheel stepping a focused number field without
/// scrolling the page, a pasted one-time code spread over its positions, a selection indicator measured from layout,
/// a rating previewed under the pointer, a toast stack laid out with CSS variables, a carousel swiped.
/// </summary>
[TestClass]
public sealed class NewComponentTests : DemoTest
{
    /// <summary>The live sample of the example titled <paramref name="title"/>.</summary>
    private static ILocator Sample(IPage page, string title) =>
        page.Locator($"{Content} .bh-example", new PageLocatorOptions { Has = page.Locator("h2", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(title)}\\s*$") }) })
            .Locator(".bh-example-panel .demo-row").First;

    [TestMethod]
    public async Task Number_field_refuses_letters_steps_with_keys_and_commits_on_blur()
    {
        var page = await OpenDemoAsync("/number-field");
        var input = Sample(page, "Quantity with steppers").Locator("input[role=spinbutton]");
        await input.ClickAsync();
        await input.PressAsync("ArrowUp");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "2");

        await input.SelectTextAsync();
        await page.Keyboard.TypeAsync("1x2");
        // "x" never reaches the input.
        await Expect(input).ToHaveValueAsync("12");
        await page.Keyboard.PressAsync("Tab");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "12");

        // Clamped to Max on commit.
        await input.FillAsync("99");
        await input.BlurAsync();
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "20");
        await Expect(Sample(page, "Quantity with steppers").Locator("button[aria-label=Increase]")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Wheel_steps_a_focused_number_field_without_scrolling_the_page()
    {
        var page = await OpenDemoAsync("/number-field");
        var input = Sample(page, "Scrub area and wheel").Locator("input[role=spinbutton]");
        await input.ScrollIntoViewIfNeededAsync();
        await input.FocusAsync();
        var scrollBefore = await page.EvaluateAsync<double>("() => window.scrollY");
        var box = (await input.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.WheelAsync(0, -100);
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "321");
        Assert.AreEqual(scrollBefore, await page.EvaluateAsync<double>("() => window.scrollY"), 0.5);
    }

    [TestMethod]
    public async Task A_pasted_code_spreads_over_the_pin_positions()
    {
        var page = await OpenDemoAsync("/pin-input");
        var first = Sample(page, "One-time code").Locator("input").First;
        await first.FocusAsync();
        await page.EvaluateAsync(
            """
            () => {
              const input = document.activeElement;
              input.value = '482913';
              input.dispatchEvent(new Event('input', { bubbles: true }));
            }
            """);
        var slots = Sample(page, "One-time code").Locator("input");
        await Expect(slots.Nth(5)).ToHaveValueAsync("3");
        await Expect(Sample(page, "One-time code").Locator("[role=group]")).ToHaveAttributeAsync("data-complete", "");

        // A letter typed into a numeric position is refused.
        await slots.Nth(5).FocusAsync();
        await page.Keyboard.TypeAsync("a");
        await Expect(slots.Nth(5)).ToHaveValueAsync("3");
    }

    [TestMethod]
    public async Task Selection_indicator_follows_the_selected_tab()
    {
        var page = await OpenDemoAsync("/tabs");
        var sample = Sample(page, "Sliding indicator and segmented control");
        var indicator = sample.Locator(".tab-underline");
        await Expect(indicator).ToHaveAttributeAsync("data-measured", "");

        double Var(string value) => double.Parse(value.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);

        await sample.Locator("[role=tab]").Nth(2).ClickAsync();
        var third = await sample.Locator("[role=tab]").Nth(2).EvaluateAsync<double[]>(
            "t => { const r = t.getBoundingClientRect(), l = t.parentElement.getBoundingClientRect(); return [r.left - l.left - t.parentElement.clientLeft, r.width]; }");
        await PollAsync(async () => Var(await indicator.EvaluateAsync<string>("i => i.style.getPropertyValue('--bit-bh-indicator-left')")),
            left => Math.Abs(left - third[0]) < 1, "the indicator over the third tab");
        Assert.AreEqual(third[1], Var(await indicator.EvaluateAsync<string>("i => i.style.getPropertyValue('--bit-bh-indicator-width')")), 1);
        await Expect(indicator).ToHaveAttributeAsync("data-activation-direction", "right");
    }

    [TestMethod]
    public async Task Rating_previews_the_hovered_value_and_sets_it_on_click()
    {
        var page = await OpenDemoAsync("/rating");
        var sample = Sample(page, "Half stars");
        var stars = sample.Locator("[data-index]");
        var fourth = (await stars.Nth(3).BoundingBoxAsync())!;
        // The right half of the fourth star is 4; the left half is 3.5.
        await page.Mouse.MoveAsync(fourth.X + fourth.Width * 0.8f, fourth.Y + fourth.Height / 2);
        await Expect(stars.Nth(3)).ToHaveAttributeAsync("data-state", "full");
        await Expect(stars.Nth(3)).ToHaveAttributeAsync("data-highlighted", "");
        await page.Mouse.MoveAsync(fourth.X + fourth.Width * 0.2f, fourth.Y + fourth.Height / 2);
        await Expect(stars.Nth(3)).ToHaveAttributeAsync("data-state", "half");
        // The sample starts at 3.5: a click there would clear it. The left half of the second star is 1.5.
        var second = (await stars.Nth(1).BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(second.X + second.Width * 0.2f, second.Y + second.Height / 2);
        await Expect(sample.Locator("[role=slider]")).ToHaveAttributeAsync("aria-valuenow", "1.5");
    }

    [TestMethod]
    public async Task Toasts_are_laid_out_as_a_stack_and_updated_in_place()
    {
        var page = await OpenDemoAsync("/toast");
        var sample = Sample(page, "Promise toasts, updates and swiping");
        await sample.GetByRole(AriaRole.Button, new() { Name = "Save (succeeds)" }).ClickAsync();
        var toast = page.Locator(".toast-viewport [data-bit-bh-toast]").Last;
        await Expect(toast).ToHaveAttributeAsync("data-variant", "loading");
        await Expect(toast).ToHaveAttributeAsync("data-front", "");
        await Expect(toast).ToHaveAttributeAsync("data-variant", "success", new() { Timeout = ExpectTimeout });
        await Expect(toast).ToContainTextAsync("Saved 3 changes");
        Assert.AreEqual("0", await toast.EvaluateAsync<string>("t => t.style.getPropertyValue('--bit-bh-toast-index')"));
        Assert.EndsWith("px", await toast.EvaluateAsync<string>("t => t.style.getPropertyValue('--bit-bh-toast-height')"));
    }

    [TestMethod]
    public async Task A_toast_swiped_far_enough_is_dismissed()
    {
        var page = await OpenDemoAsync("/toast");
        await Sample(page, "Simple toast").Locator("button").First.ClickAsync();
        var toast = page.Locator(".toast-viewport [data-bit-bh-toast]").Last;
        await Expect(toast).ToBeVisibleAsync();
        var box = (await toast.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 20, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 60, box.Y + box.Height / 2, new() { Steps = 4 });
        await Expect(toast).ToHaveAttributeAsync("data-swipe", "move");
        await page.Mouse.MoveAsync(box.X + 160, box.Y + box.Height / 2, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        await Expect(page.Locator(".toast-viewport [data-bit-bh-toast]")).ToHaveCountAsync(0, new() { Timeout = ExpectTimeout });
    }

    [TestMethod]
    public async Task Tags_input_moves_from_the_empty_input_to_the_tags_and_removes_them()
    {
        var page = await OpenDemoAsync("/tags-input");
        var sample = Sample(page, "Keywords");
        var input = sample.Locator("input");
        await input.FillAsync("css, html,");
        await Expect(sample.Locator("[role=row]")).ToHaveCountAsync(4);
        await input.PressAsync("Backspace");
        await PollAsync(async () => (await FocusedAsync(page))?.Role, "row", "the focus on the last tag");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(sample.Locator("[role=row]")).ToHaveCountAsync(3);
        await Expect(page.Locator("[data-bit-bh-live-region]", new() { HasText = "html removed" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_press_in_the_color_area_moves_the_thumb_there()
    {
        var page = await OpenDemoAsync("/color-picker");
        var areaBox = (await Sample(page, "Area, hue, alpha, swatch and field").Locator(".color-area").BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(areaBox.X + areaBox.Width * 0.25f, areaBox.Y + areaBox.Height * 0.75f);
        var thumb = Sample(page, "Area, hue, alpha, swatch and field").Locator(".color-area [role=slider]");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuetext", new Regex("Saturation 2[45]%, Brightness 2[45]%"));
        await Expect(thumb).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuetext", new Regex("Brightness 2[56]%"));
    }

    [TestMethod]
    public async Task A_selection_anchored_listbox_lines_the_selected_option_up_over_its_button()
    {
        var page = await OpenDemoAsync("/listbox");
        var sample = Sample(page, "Aligned with the selection");
        var button = sample.Locator("[role=combobox]");
        await button.ScrollIntoViewIfNeededAsync();
        await button.ClickAsync();
        var listbox = sample.Locator("[role=listbox]");
        await Expect(listbox).ToHaveAttributeAsync("data-side", "none");
        var buttonBox = (await button.BoundingBoxAsync())!;
        var selected = (await listbox.Locator("[aria-selected=true]").BoundingBoxAsync())!;
        Assert.AreEqual(buttonBox.Y + buttonBox.Height / 2, selected.Y + selected.Height / 2, 2);
    }

    [TestMethod]
    public async Task A_column_resizer_drags_its_column_wider()
    {
        var page = await OpenDemoAsync("/grid");
        var sample = Sample(page, "Resizable columns");
        var handle = sample.Locator("[role=separator]").First;
        await handle.ScrollIntoViewIfNeededAsync();
        var header = sample.Locator("th").First;
        var before = (await header.BoundingBoxAsync())!.Width;
        var box = (await handle.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2 + 60, box.Y + box.Height / 2, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await PollAsync(async () => (await header.BoundingBoxAsync())!.Width, w => w > before + 40, "the column 60px wider");
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "220");
    }

    [TestMethod]
    public async Task F6_moves_the_focus_from_landmark_to_landmark()
    {
        var page = await OpenDemoAsync("/accessibility");
        await page.Locator($"{Content} h1").ClickAsync();
        var landmarks = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            await page.Keyboard.PressAsync("F6");
            landmarks.Add(await page.EvaluateAsync<string>(
                """
                () => {
                  const a = document.activeElement;
                  const l = a && a.closest('main,[role=main],nav,[role=navigation],aside,header,footer,[role=region],section[aria-label],section[aria-labelledby]');
                  return l ? (l.id || l.tagName + ':' + (l.getAttribute('aria-label') || '')) : '';
                }
                """));
        }
        Assert.IsTrue(landmarks.All(l => l.Length > 0), string.Join(" | ", landmarks));
        Assert.IsGreaterThan(1, landmarks.Distinct().Count(), string.Join(" | ", landmarks));
    }

    [TestMethod]
    public async Task Swiping_the_carousel_shows_the_next_slide()
    {
        var page = await OpenDemoAsync("/carousel");
        var sample = Sample(page, "Several slides in view");
        var track = sample.Locator(".carousel-track");
        await track.ScrollIntoViewIfNeededAsync();
        var box = (await track.BoundingBoxAsync())!;
        var y = box.Y + box.Height / 2;
        await page.Mouse.MoveAsync(box.X + box.Width * 0.7f, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width * 0.3f, y, new() { Steps = 6 });
        await page.Mouse.UpAsync();
        await Expect(sample.Locator("section")).ToHaveAttributeAsync("data-index", "1");
    }

    [TestMethod]
    public async Task A_press_and_drag_on_the_dial_point_the_angle_at_the_pointer()
    {
        var page = await OpenDemoAsync("/angle-slider");
        var sample = Sample(page, "Rotation");
        var dial = sample.Locator(".dial");
        await dial.ScrollIntoViewIfNeededAsync();
        var box = (await dial.BoundingBoxAsync())!;
        float cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2;
        var thumb = sample.Locator("[role=slider]");

        // 6 o'clock.
        await page.Mouse.ClickAsync(cx, cy + box.Height * 0.4f);
        await Expect(thumb).ToHaveAttributeAsync("aria-valuenow", "180");
        await Expect(thumb).ToBeFocusedAsync();

        // Dragged round to 9 o'clock.
        await page.Mouse.MoveAsync(cx, cy + box.Height * 0.4f);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(cx - box.Width * 0.4f, cy, new() { Steps = 6 });
        await page.Mouse.UpAsync();
        await Expect(thumb).ToHaveAttributeAsync("aria-valuenow", "270");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuetext", "270 degrees");

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(thumb).ToHaveAttributeAsync("aria-valuenow", "271");
    }

    [TestMethod]
    public async Task A_slider_marker_lines_up_with_the_thumb_at_its_value()
    {
        var page = await OpenDemoAsync("/slider");
        var sample = Sample(page, "Tick marks");
        var thumb = sample.Locator("[role=slider]");
        await thumb.ScrollIntoViewIfNeededAsync();
        var marker = sample.Locator("[data-state=at-value]");
        await Expect(marker).ToHaveAttributeAsync("data-value", "50");
        var thumbBox = (await thumb.BoundingBoxAsync())!;
        var markerBox = (await marker.BoundingBoxAsync())!;
        Assert.AreEqual(thumbBox.X + thumbBox.Width / 2, markerBox.X + markerBox.Width / 2, 1.5);

        await thumb.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(sample.Locator("[data-state=at-value]")).ToHaveAttributeAsync("data-value", "75");
    }
}
