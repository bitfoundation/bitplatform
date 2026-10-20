using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// Choosing from a list: a combobox that takes a label and stores the value behind it, cities loaded for the chosen
/// country and reloaded when it changes, a change handler that fills in a sibling the user may then edit, chips added
/// with Enter or a comma and taken back with Backspace, and an options service that fails without taking the form down.
/// </summary>
[TestClass]
public sealed class ChoiceTests : BormTest
{
    [TestMethod]
    public async Task Typing_a_label_into_the_combobox_stores_the_value_it_stands_for()
    {
        var page = await OpenAsync("/combobox-tags");
        var country = Control(page, "Country");

        // The datalist offers labels, because that is what the browser puts in the box.
        await Expect(page.Locator("#borm_Country_list > option")).ToHaveCountAsync(4);

        // Any casing of a label is that option: the box shows its label, the model holds its value.
        await EnterAsync(country, "germany");
        await Expect(country).ToHaveValueAsync("Germany");
        await Expect(Errors(page, "Country")).ToHaveCountAsync(0);

        await EnterAsync(Control(page, "FullName"), "Grace Hopper");
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("\"Country\": \"de\"");

        // An answer that is on no list is kept in the box, next to the message saying so.
        await EnterAsync(country, "Atlantis");
        await Expect(country).ToHaveValueAsync("Atlantis");
        await Expect(Errors(page, "Country")).ToHaveTextAsync("Choose one of the available options.");
        await Expect(country).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task Choosing_a_country_loads_its_cities_and_changing_it_clears_the_old_city()
    {
        var page = await OpenAsync("/combobox-tags");
        var cities = page.Locator("#borm_City_list > option");

        await Expect(cities).ToHaveCountAsync(0);

        await EnterAsync(Control(page, "Country"), "Japan");
        await Expect(cities).ToHaveCountAsync(3);
        Assert.AreEqual("Tokyo,Osaka,Kyoto", string.Join(",", await OptionValuesAsync(cities)));

        // This one accepts a city that is not on the list, too.
        await EnterAsync(Control(page, "City"), "Nara");
        await Expect(Errors(page, "City")).ToHaveCountAsync(0);

        // A new country invalidates the city (the change handler clears it) and the provider reloads the list.
        await EnterAsync(Control(page, "Country"), "France");
        await Expect(Control(page, "City")).ToHaveValueAsync("");
        await EventuallyAsync(async () => string.Join(",", await OptionValuesAsync(cities)) == "Paris,Lyon,Marseille",
            "the city list was reloaded for France");
        await Expect(page.Locator("#borm_City ~ [aria-live=polite]")).ToHaveTextAsync("3 choices available");
    }

    [TestMethod]
    public async Task Choosing_a_plan_fills_in_the_seats_which_stay_editable()
    {
        var page = await OpenAsync("/combobox-tags");
        var plan = Control(page, "Plan");
        var seats = Control(page, "Seats");

        await Expect(seats).ToHaveValueAsync("1");
        await plan.GetByLabel("Team", new() { Exact = true }).CheckAsync();
        await Expect(seats).ToHaveValueAsync("5");
        await plan.GetByLabel("Enterprise", new() { Exact = true }).CheckAsync();
        await Expect(seats).ToHaveValueAsync("25");

        // The handler wrote a value, it did not bind one: the user's own number stays.
        await EnterAsync(seats, "40");
        await WaitForQuietAsync(page);
        await Expect(seats).ToHaveValueAsync("40");
        await Expect(plan.GetByLabel("Enterprise", new() { Exact = true })).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Enter_or_a_comma_adds_a_tag_and_Backspace_on_an_empty_box_removes_the_last()
    {
        var page = await OpenAsync("/combobox-tags");
        var entry = page.Locator("#borm_Skills_entry");
        var tags = page.Locator("#borm_Skills .bit-borm-tag__label");

        await entry.PressSequentiallyAsync("Blazor");
        await entry.PressAsync("Enter");
        await Expect(tags).ToHaveTextAsync(["Blazor"]);
        await Expect(entry).ToHaveValueAsync("");
        await Expect(entry).ToBeFocusedAsync();

        // Enter in the tag box adds a tag and nothing else: the box belongs to no form, so it cannot submit this
        // one - which, with the required name empty, would have put up the error summary.
        await Expect(Summary(page)).ToHaveCountAsync(0);

        // A comma commits what is before it, so typing and pasting a list behave alike; a repeat in any casing is
        // the same tag and is not added twice.
        await entry.PressSequentiallyAsync("C#, Testing,blazor,");
        await Expect(tags).ToHaveTextAsync(["Blazor", "C#", "Testing"]);
        await Expect(page.Locator("#borm_Skills [aria-live=polite]")).ToHaveTextAsync("3 selected");

        // Backspace on an empty box takes the last tag back; with text in the box it edits the text.
        await entry.PressSequentiallyAsync("x");
        await entry.PressAsync("Backspace");
        await Expect(tags).ToHaveTextAsync(["Blazor", "C#", "Testing"]);
        await entry.PressAsync("Backspace");
        await Expect(tags).ToHaveTextAsync(["Blazor", "C#"]);

        // Each chip's own button removes that chip, and says which one it removes.
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove Blazor", Exact = true }).ClickAsync();
        await Expect(tags).ToHaveTextAsync(["C#"]);

        await EnterAsync(Control(page, "FullName"), "Grace Hopper");
        await EnterAsync(Control(page, "Country"), "Japan");
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("\"Skills\": [");
        await Expect(Result(page)).ToContainTextAsync("\"C#\"");
    }

    [TestMethod]
    public async Task Changing_the_country_reloads_the_cities_and_drops_a_city_that_no_longer_exists()
    {
        var page = await OpenAsync("/cascading");
        var city = Control(page, "City");

        await Control(page, "Country").SelectOptionAsync("gb");
        await EventuallyAsync(async () => string.Join(",", await OptionValuesAsync(city.Locator("option:not([value=''])"))) == "London,Manchester,Bristol",
            "the cities of the United Kingdom were loaded");
        await Expect(city).ToBeEnabledAsync();
        await city.SelectOptionAsync("Bristol");
        await Expect(city).ToHaveValueAsync("Bristol");

        // While the lookup runs the list says so and cannot be used; then it holds the new country's cities, and
        // the old selection - which does not exist there - is gone. The list is watched from before the change, so
        // the short loading state cannot slip between two polls.
        await city.EvaluateAsync(
            """
            el => {
              window.__sawLoading = false;
              new MutationObserver(() => {
                if (el.disabled && el.getAttribute('aria-busy') === 'true' && el.options[0]?.text === 'Loading…') window.__sawLoading = true;
              }).observe(el, { attributes: true, childList: true, subtree: true, characterData: true });
            }
            """);
        await Control(page, "Country").SelectOptionAsync("de");
        await EventuallyAsync(async () => string.Join(",", await OptionValuesAsync(city.Locator("option:not([value=''])"))) == "Berlin,Hamburg,Munich",
            "the cities of Germany were loaded");
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => window.__sawLoading"), "the city list never said it was loading");
        await Expect(city).ToBeEnabledAsync();
        await Expect(city).ToHaveValueAsync("");
        await Expect(city).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    [TestMethod]
    public async Task A_failing_options_service_says_so_and_the_form_recovers_once_it_is_back()
    {
        var page = await OpenAsync("/composition");
        var component = Control(page, "Component", "custom_");

        await page.Locator("label", new() { HasTextString = "Break the component service" }).Locator("input").CheckAsync();
        await Control(page, "Team", "custom_").SelectOptionAsync("billing");

        // The failure is shown in place of the choices, and reported to the page - not thrown at it.
        await Expect(component.Locator("option").First).ToHaveTextAsync("Options could not be loaded");
        await Expect(page.Locator(".bb-demo", new() { HasTextString = "OnOptionsLoadFailed" })).ToContainTextAsync("The component service is unavailable.");

        // Mended, a retry loads the team's components.
        await page.Locator("label", new() { HasTextString = "Break the component service" }).Locator("input").UncheckAsync();
        await EventuallyAsync(async () => string.Join(",", await OptionValuesAsync(component.Locator("option:not([value=''])"))) == "Invoicing,Payments",
            "the components were loaded once the service was back");
        await component.SelectOptionAsync("Payments");
        await Expect(component).ToHaveValueAsync("Payments");
    }

    private static Task<string[]> OptionValuesAsync(ILocator options) =>
        options.EvaluateAllAsync<string[]>("os => os.map(o => o.value)");
}
