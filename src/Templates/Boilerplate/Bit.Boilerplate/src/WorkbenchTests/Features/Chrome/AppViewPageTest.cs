namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>What the tests of the app inside a view share: its header, account menu, side menu, sign-in guide and snack bars.</summary>
public abstract class AppViewPageTest : WorkbenchPageTest
{
    protected ILocator Header => View().Locator(".main-body > header:not([data-bit-callout-link])");

    protected ILocator PageTitle => Header.GetByRole(AriaRole.Heading, new() { Level = 1 });

    /// <summary>The account menu's button, named after the signed-in user, or after the menu when signed out.</summary>
    protected ILocator AccountButton(string name) => Header.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator AccountMenu => View().Locator(".app-menu-card");

    protected ILocator MenuItem(string name) => AccountMenu.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator SideMenu => View().Locator(".nav-panel");

    protected ILocator SideMenuLink(string name) => SideMenu.GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Link, new() { Name = name, Exact = true });

    protected ILocator SideMenuGroup(string name) => SideMenu.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator BottomBar => View().Locator("section.nav-bar");

    protected ILocator SignInGuide => Page.GetByText(WorkbenchStrings.SignInGuide);

    protected ILocator SignInTools => Tools.Locator("[data-bonboarding-anchor='sign-in']");

    protected ILocator UserPicker => SignInTools.GetByRole(AriaRole.Combobox);

    /// <summary>A message on the view's snack bar.</summary>
    protected ILocator Snack(string text) => View().Locator(".bit-snb-ttl", new() { HasText = text }).First;

    protected async Task OpenAccountMenu()
    {
        await Press(Header.Locator(".persona").First);
        await Expect(AccountMenu).ToBeInViewportAsync();
    }

    /// <summary>Waits until a press that should have changed nothing would have shown, as opening a menu takes a trip to the server, and checks the view is still on its page.</summary>
    protected async Task ExpectTheViewStillOn(string pageKey, string title)
    {
        await OpenAccountMenu();
        await CloseAccountMenu();

        await Expect(PageTitle).ToHaveTextAsync(title);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workbench/{pageKey}"));

        Assert.StartsWith($"/preview/{pageKey}", await View().Locator("html").EvaluateAsync<string>("() => location.pathname"));
    }

    /// <summary>Waits until the account menu has closed: once opened, it stays in the page out of sight.</summary>
    protected Task ExpectTheAccountMenuClosed() => Expect(Header.GetByRole(AriaRole.Button, new() { Expanded = true })).ToHaveCountAsync(0);

    /// <summary>Closes the account menu by pressing beside it, on the overlay it draws over the page.</summary>
    protected async Task CloseAccountMenu()
    {
        var overlay = View().Locator(".bit-drm-ovl").First;
        var beside = new Position { X = 5, Y = 300 };

        if (IsPhone)
        {
            await overlay.TapAsync(new() { Position = beside });
        }
        else
        {
            await overlay.ClickAsync(new() { Position = beside });
        }

        await ExpectTheAccountMenuClosed();
    }

    /// <summary>Opens the side menu of a view narrow enough to keep it in a drawer behind the header's button.</summary>
    protected async Task OpenSideMenu()
    {
        await Press(Header.GetByRole(AriaRole.Button, new() { Name = AppStrings.OpenNavigationMenuTitle, Exact = true }));
        await Expect(SideMenu.GetByRole(AriaRole.Searchbox)).ToBeInViewportAsync();
    }

    /// <summary>Closes the side menu's drawer by pressing beside it, on its overlay.</summary>
    protected async Task CloseSideMenu()
    {
        var overlay = View().Locator(".bit-npn-ovl");
        var box = (await overlay.BoundingBoxAsync())!;
        var beside = new Position { X = box.Width - 10, Y = box.Height / 2 };

        if (IsPhone)
        {
            await overlay.TapAsync(new() { Position = beside });
        }
        else
        {
            await overlay.ClickAsync(new() { Position = beside });
        }

        await Expect(SideMenu.GetByRole(AriaRole.Searchbox)).Not.ToBeInViewportAsync();
    }

    /// <summary>Presses a button of the tools, which a phone keeps in its panel.</summary>
    protected async Task PressInTheTools(string name)
    {
        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }));
        await CloseTools();
    }

    /// <summary>Picks a demo user in the tools' user picker.</summary>
    protected async Task PickUser(string name)
    {
        await Press(UserPicker);
        await Press(Page.GetByRole(AriaRole.Option, new() { NameRegex = new Regex($"^{Regex.Escape(name)} ") }));
    }

    protected async Task ReloadTheView()
    {
        await Page.ReloadAsync();
        await WaitForViews();
    }

    /// <summary>Waits until the view has reloaded with this in its address and come alive, as a press on what the server drew first goes nowhere.</summary>
    protected Task WaitForTheViewToReloadWith(string query) => WaitForTheViewToReload(url => url.Contains(query, StringComparison.Ordinal));

    /// <summary>The same for what the reload takes out of the view's address.</summary>
    protected Task WaitForTheViewToReloadWithout(string query) => WaitForTheViewToReload(url => url.Contains(query, StringComparison.Ordinal) is false);

    private async Task WaitForTheViewToReload(Func<string, bool> isTheNewAddress)
    {
        await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(Page.Frames.Any(frame => frame.Url.Contains("/preview/", StringComparison.Ordinal) && isTheNewAddress(frame.Url))), isThere => isThere);
        await WaitForViews();
    }

    /// <summary>Reloads once the view has saved this part of its stores' JSON, as a change reaches the browser's storage a moment after the page shows it.</summary>
    protected async Task ReloadTheViewOnceSaved(string saved)
    {
        await WaitUntilSaved(saved);
        await ReloadTheView();
    }

    /// <summary>Waits until the view has saved this part of its stores' JSON.</summary>
    protected Task WaitUntilSaved(string saved) => WorkbenchTestReviewer.WaitFor(ReadTheViewsSavedState, state => state.Contains(saved, StringComparison.Ordinal));

    private Task<string> ReadTheViewsSavedState() => View().Locator("html").EvaluateAsync<string>("""
        () => new Promise((resolve, reject) => {
            const opening = indexedDB.open('bit-workbench');
            opening.onerror = () => reject(opening.error);
            opening.onsuccess = () => {
                const reading = opening.result.transaction('state').objectStore('state').getAll();
                reading.onerror = () => reject(reading.error);
                reading.onsuccess = () => {
                    opening.result.close();
                    resolve(reading.result.join());
                };
            };
        })
        """);

    /// <summary>Moves a finger over the screen at a finger's pace from one point to another, after holding it down as long as the gesture needs.</summary>
    protected async Task SwipeWithAFinger((double X, double Y) from, (double X, double Y) to, int holdMilliseconds = 0)
    {
        var cdp = await Context.NewCDPSessionAsync(Page);

        Task Touch(string type, params (double X, double Y)[] points) => cdp.SendAsync("Input.dispatchTouchEvent", new()
        {
            ["type"] = type,
            ["touchPoints"] = points.Select(point => new Dictionary<string, object> { ["x"] = point.X, ["y"] = point.Y }).ToArray()
        });

        await Touch("touchStart", from);

        if (holdMilliseconds > 0)
        {
            await Page.WaitForTimeoutAsync(holdMilliseconds);
        }

        foreach (var step in Enumerable.Range(1, 10))
        {
            await Touch("touchMove", (from.X + (to.X - from.X) * step / 10, from.Y + (to.Y - from.Y) * step / 10));
            await Page.WaitForTimeoutAsync(16);
        }

        await Touch("touchEnd");
    }

    /// <summary>The app's text in another language, as <see cref="WorkbenchPageTest.InCulture"/> does for the Workbench's.</summary>
    protected static string AppInCulture(string culture, string key) => AppStrings.ResourceManager.GetString(key, CultureInfo.GetCultureInfo(culture))!;
}
