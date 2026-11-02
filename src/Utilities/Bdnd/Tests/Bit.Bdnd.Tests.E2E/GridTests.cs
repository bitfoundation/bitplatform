using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The sortable grid: four tiles a row, each landing left or right of the tile it is dropped on. With the keyboard,
/// Left and Right step through the gaps and Up and Down go a row up or down, to the gap closest across.
/// </summary>
[TestClass]
public sealed class GridTests : BdndTest
{
    private static ILocator Tile(IPage page, string name) =>
        page.Locator(".grid-tile:not([data-bit-bdnd-preview])", new() { HasText = name });

    private static ILocator Slot(IPage page, string name) => page.Locator(".grid-slot", new() { Has = Tile(page, name) });

    private static ILocator Order(IPage page) => page.Locator(".grid-order");

    [TestMethod]
    public async Task A_tile_dropped_on_the_right_half_of_a_tile_in_the_next_row_lands_after_it()
    {
        var page = await OpenAsync("/sortable/grid");
        var fuchsia = await BoxAsync(Tile(page, "Fuchsia"));

        await StartDragAsync(page, Tile(page, "Amber"));
        await DragToAsync(page, new Point(fuchsia.X + fuchsia.Width * 0.8, fuchsia.Centre.Y),
            () => Expect(Slot(page, "Fuchsia")).ToHaveAttributeAsync("data-bit-bdnd-edge", "right"));

        await Expect(Order(page)).ToHaveTextAsync("Order: Blue, Coral, Denim, Emerald, Fuchsia, Amber, Gold, Hazel, Indigo, Jade, Khaki, Lilac");
    }

    [TestMethod]
    public async Task Down_moves_a_tile_a_row_down_and_keeps_its_column()
    {
        var page = await OpenAsync("/sortable/grid");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Tile(page, "Blue").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        // the second column of the next row: after Fuchsia, which Blue then replaces there
        await Expect(live).ToHaveTextAsync("Blue is after Fuchsia.");
        await Expect(Slot(page, "Fuchsia")).ToHaveAttributeAsync("data-bit-bdnd-edge", "right");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Blue is after Jade.");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(live).ToHaveTextAsync("Blue is after Fuchsia.");
        await page.Keyboard.PressAsync("Space");

        await Expect(Order(page)).ToHaveTextAsync("Order: Amber, Coral, Denim, Emerald, Fuchsia, Blue, Gold, Hazel, Indigo, Jade, Khaki, Lilac");
        await EventuallyAsync(() => Tile(page, "Blue").EvaluateAsync<bool>("el => document.activeElement === el"), "the moved tile keeps the focus");
    }

    [TestMethod]
    public async Task Up_moves_a_tile_a_row_up_and_left_and_right_step_one_gap()
    {
        var page = await OpenAsync("/sortable/grid");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Tile(page, "Jade").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        // the second column of the row above: before Fuchsia - the gap after Emerald
        await Expect(live).ToHaveTextAsync("Jade is after Emerald.");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync("Jade is after Fuchsia.");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(live).ToHaveTextAsync("Jade is after Emerald.");
        await page.Keyboard.PressAsync("Space");

        await Expect(Order(page)).ToHaveTextAsync("Order: Amber, Blue, Coral, Denim, Emerald, Jade, Fuchsia, Gold, Hazel, Indigo, Khaki, Lilac");
    }

    [TestMethod]
    public async Task Up_from_the_first_column_lands_at_the_start_of_the_row_above()
    {
        var page = await OpenAsync("/sortable/grid");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Tile(page, "Indigo").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        // the gap where the second row breaks from the first is shown, and offered, at the start of the second row
        await Expect(live).ToHaveTextAsync("Indigo is before Emerald.");
        await Expect(Slot(page, "Emerald")).ToHaveAttributeAsync("data-bit-bdnd-edge", "left");
        await page.Keyboard.PressAsync("Space");

        await Expect(Order(page)).ToHaveTextAsync("Order: Amber, Blue, Coral, Denim, Indigo, Emerald, Fuchsia, Gold, Hazel, Jade, Khaki, Lilac");
    }

    [TestMethod]
    public async Task Down_from_the_start_of_a_row_keeps_to_the_first_column()
    {
        var page = await OpenAsync("/sortable/grid");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Tile(page, "Blue").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("Home");
        await Expect(live).ToHaveTextAsync("Blue is before Amber.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Blue is before Emerald.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("Blue is before Indigo.");
        await page.Keyboard.PressAsync("Escape");

        await Expect(Order(page)).ToHaveTextAsync("Order: Amber, Blue, Coral, Denim, Emerald, Fuchsia, Gold, Hazel, Indigo, Jade, Khaki, Lilac");
    }

    [TestMethod]
    public async Task Down_on_the_last_row_goes_nowhere()
    {
        var page = await OpenAsync("/sortable/grid");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Tile(page, "Jade").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowDown");
        // no row below, and nothing after this row in the page: not sideways along the row
        await Expect(live).ToHaveTextAsync("There is no drop target in that direction.");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync("Jade is after Khaki.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync("There is nothing further in that direction: Jade stays after Khaki.");
        await page.Keyboard.PressAsync("Escape");

        await Expect(Order(page)).ToHaveTextAsync("Order: Amber, Blue, Coral, Denim, Emerald, Fuchsia, Gold, Hazel, Indigo, Jade, Khaki, Lilac");
    }
}
