namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>What the tests of the showcase pages share: a reload of the view, the tools, the view's own controls and a drag.</summary>
public abstract class ShowcaseUITest : WorkbenchPageTest
{
    /// <summary>A button of the view by its exact name.</summary>
    protected ILocator ViewButton(string name) => View().GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>Comes back to the page by reloading the shell and its view, once the view's stores have finished saving in the browser.</summary>
    protected async Task ReloadTheView()
    {
        await View().Locator("body").EvaluateAsync("""
            async () => {
                const read = () => new Promise(resolve => {
                    const opening = indexedDB.open('bit-workbench');
                    opening.onerror = () => resolve('');
                    opening.onsuccess = () => {
                        const database = opening.result;
                        if (database.objectStoreNames.contains('state') === false) { database.close(); resolve(''); return; }
                        const reading = database.transaction('state').objectStore('state').getAll();
                        reading.onsuccess = () => { database.close(); resolve(JSON.stringify(reading.result)); };
                        reading.onerror = () => { database.close(); resolve(''); };
                    };
                });
                let last = await read();
                let lastChange = performance.now();
                while (performance.now() - lastChange < 300) {
                    await new Promise(resolve => setTimeout(resolve, 50));
                    const now = await read();
                    if (now !== last) { last = now; lastChange = performance.now(); }
                }
            }
            """);

        await Page.ReloadAsync();
        await WaitForViews();
    }

    /// <summary>Opens a page without waiting for it to settle, to see what it shows until then.</summary>
    protected Task OpenWithoutWaiting(string pageKey, string query) => Page.GotoAsync(new Uri(Server.Address, $"workbench/{pageKey}{query}").ToString());

    /// <summary>Presses a button of the selected view's settings, which reloads the view.</summary>
    protected async Task UseTheTools(string button)
    {
        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = button, Exact = true }));
        await CloseTools();
        await WaitForViews();
    }

    /// <summary>Brings an element into sight and waits for the page's smooth scroll to end, as a popup opened on a moving page closes again.</summary>
    protected async Task ScrollToAndWaitUntilStill(ILocator element)
    {
        await element.ScrollIntoViewIfNeededAsync();

        await element.EvaluateAsync("""
            element => new Promise(resolve => {
                let last = element.getBoundingClientRect().top;
                let stillFrames = 0;
                const check = () => {
                    const now = element.getBoundingClientRect().top;
                    stillFrames = now === last ? stillFrames + 1 : 0;
                    last = now;
                    stillFrames >= 10 ? resolve() : requestAnimationFrame(check);
                };
                requestAnimationFrame(check);
            })
            """);
    }

    /// <summary>Picks an option from a dropdown of the view, in its list or its phone panel.</summary>
    protected async Task Pick(ILocator dropdown, string option)
    {
        await Press(dropdown);
        await Press(VisibleOption(option));
    }

    /// <summary>The dropdowns, calendars and clocks that are open, which the components move out to the end of the page.</summary>
    protected ILocator OpenPopups => View().Locator("[data-bit-callout-wrapper]");

    /// <summary>An option of the dropdown that is open.</summary>
    protected ILocator VisibleOption(string option) => OpenPopups.GetByRole(AriaRole.Option, new() { Name = option, Exact = true });

    /// <summary>A notice of the view, like a snack bar, with this text.</summary>
    protected ILocator Notice(string text) => View().GetByRole(AriaRole.Status).Filter(new() { HasText = text });

    /// <summary>Checks that the view's page fits the screen, with nothing to scroll sideways to.</summary>
    protected async Task ExpectNoSidewaysScroll()
    {
        var overflow = await View().Locator("html").EvaluateAsync<int>("html => html.scrollWidth - html.clientWidth");

        Assert.IsLessThanOrEqualTo(1, overflow, "The page scrolls sideways.");
    }

    /// <summary>Drags from one element to a spot on another: with the mouse on a desktop, and on a phone with a finger that first holds as a long press does.</summary>
    protected async Task Drag(ILocator from, ILocator to, double toX = 0.5, double toY = 0.5)
    {
        await from.ScrollIntoViewIfNeededAsync();

        var start = (await from.BoundingBoxAsync())!;
        var end = (await to.BoundingBoxAsync())!;
        var (startX, startY) = (start.X + start.Width / 2, start.Y + start.Height / 2);
        var (endX, endY) = (end.X + end.Width * toX, end.Y + end.Height * toY);

        if (IsPhone)
        {
            var cdp = await Page.Context.NewCDPSessionAsync(Page);

            await Touch(cdp, "touchStart", startX, startY);
            await Page.WaitForTimeoutAsync(600);

            for (var step = 1; step <= 12; step++)
            {
                await Touch(cdp, "touchMove", startX + (endX - startX) * step / 12, startY + (endY - startY) * step / 12);
                await Page.WaitForTimeoutAsync(16);
            }

            await Touch(cdp, "touchEnd", endX, endY);
        }
        else
        {
            await Page.Mouse.MoveAsync((float)startX, (float)startY);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync((float)endX, (float)endY, new() { Steps = 12 });
            await Page.Mouse.MoveAsync((float)endX, (float)endY);
            await Page.Mouse.UpAsync();
        }
    }

    /// <summary>Presses a spot twice in a row, as this surface does: a double click on a desktop and two quick taps on a phone.</summary>
    protected async Task DoublePressAt(float x, float y)
    {
        if (IsPhone)
        {
            await Page.Touchscreen.TapAsync(x, y);
            await Page.Touchscreen.TapAsync(x, y);
        }
        else
        {
            await Page.Mouse.DblClickAsync(x, y);
        }
    }

    /// <summary>Presses the middle of an element twice in a row.</summary>
    protected async Task DoublePress(ILocator target)
    {
        await target.ScrollIntoViewIfNeededAsync();

        var box = (await target.BoundingBoxAsync())!;

        await DoublePressAt(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    protected static Task Touch(ICDPSession cdp, string type, double x, double y) => cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
    {
        ["type"] = type,
        ["touchPoints"] = type == "touchEnd" ? Array.Empty<object>() : new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 1 } }
    });
}
