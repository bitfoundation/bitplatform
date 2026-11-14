using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// A multi-drag says how many items it carries on its picture: the copy of the item the browser takes its drag image
/// of (which carries <c>data-bortable-count</c> and the count while the browser takes it) and the preview that follows
/// a finger. The dragged item itself is never marked. A drag of one item carries no count.
/// </summary>
[TestClass]
public sealed class MultiDragCountTests : BortableTest
{
    // Records the count on the drag image when the browser took it (after every dragstart handler), and whether the
    // dragged item itself was marked.
    private const string RecordCounts = """
        () => {
          window.__counts = [];
          const picture = () => document.querySelector('[data-bortable-count]');
          window.addEventListener('dragstart', e => {
            const item = e.target.closest('[data-bortable-item]');
            const image = picture();
            window.__counts.push(image ? image.getAttribute('data-bortable-count') + ':' + image.querySelector('.bit-bortable-count')?.textContent : 'none');
            window.__counts.push('item:' + (item?.hasAttribute('data-bortable-count') ? 'marked' : 'clean'));
            setTimeout(() => window.__counts.push('after:' + (picture()?.getAttribute('data-bortable-count') ?? 'none')), 50);
          });
        }
        """;

    [TestMethod]
    public async Task A_native_drag_of_a_selection_shows_how_many_items_it_carries()
    {
        var page = await OpenAsync("/multi-drag");
        await page.EvaluateAsync(RecordCounts);
        await Item(page, "multi-list", "Item 2").ClickAsync();
        await Item(page, "multi-list", "Item 4").ClickAsync();
        await Item(page, "multi-list", "Item 5").ClickAsync();
        await Expect(page.Locator("#multi-list > [data-bortable-selected]")).ToHaveTextAsync(["Item 2", "Item 4", "Item 5"]);

        await DragAsync(page,
            (await BoxAsync(Item(page, "multi-list", "Item 2"))).Centre,
            (await BoxAsync(Item(page, "multi-list", "Item 6"))).Centre);

        // The count is on the drag image while the browser takes it, and gone right after; the item is left alone.
        await AssertOrderAsync(page, "multi-list", "multi-order", "Item 1", "Item 3", "Item 6", "Item 2", "Item 4", "Item 5");
        CollectionAssert.AreEqual(new[] { "3:3", "item:clean", "after:none" }, await page.EvaluateAsync<string[]>("() => window.__counts"));
        await Expect(page.Locator("[data-bortable-count]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_drag_of_one_item_carries_no_count()
    {
        var page = await OpenAsync("/multi-drag");
        await page.EvaluateAsync(RecordCounts);

        await DragAsync(page,
            (await BoxAsync(Item(page, "multi-list", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "multi-list", "Item 3"))).At(0.5, 0.7));

        Assert.AreEqual("none", (await page.EvaluateAsync<string[]>("() => window.__counts"))[0]);
    }

    [TestMethod]
    public async Task A_finger_dragging_a_selection_shows_the_count_on_its_preview()
    {
        if (E2EEnvironment.IsChromium is false)
            Assert.Inconclusive($"Touch input is sent through the Chrome DevTools Protocol, which {E2EEnvironment.Browser} does not speak.");

        var page = await OpenAsync("/multi-drag", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 1400 },
            DeviceScaleFactor = 1,
            HasTouch = true,
        });
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await Item(page, "multi-list", "Item 1").ClickAsync();
        await Item(page, "multi-list", "Item 3").ClickAsync();
        await Expect(page.Locator("#multi-list > [data-bortable-selected]")).ToHaveCountAsync(2);

        var from = (await BoxAsync(Item(page, "multi-list", "Item 1"))).Centre;
        var to = (await BoxAsync(Item(page, "multi-list", "Item 5"))).At(0.5, 0.7);
        await TouchAsync(cdp, "touchStart", from);
        await Task.Delay(400); // longer than TouchDelay
        for (int i = 1; i <= 12; i++)
        {
            await TouchAsync(cdp, "touchMove", new Point(from.X + (to.X - from.X) * i / 12, from.Y + (to.Y - from.Y) * i / 12));
            await Task.Delay(16);
        }
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".bit-bortable-drag")).ToHaveAttributeAsync("data-bortable-count", "2");
        // The preview is a picture: it carries no state of the item's own.
        await Expect(page.Locator(".bit-bortable-drag")).Not.ToHaveAttributeAsync("data-bortable-selected", "true");

        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);
        await Expect(page.Locator(".bit-bortable-drag")).ToHaveCountAsync(0);
    }

    private static Task TouchAsync(ICDPSession cdp, string type, Point at) =>
        cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = type == "touchEnd"
                ? Array.Empty<object>()
                : new object[] { new Dictionary<string, object> { ["x"] = at.X, ["y"] = at.Y, ["id"] = 1 } },
        });
}
