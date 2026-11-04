namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>What the shell's browser tests share: a view's real size, the buildings page's search, what the browser saved and a drag.</summary>
public abstract class ShellPageTest : WorkbenchPageTest
{
    protected ILocator BuildingsSearch(string paneId = "1") => View(paneId).GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch });

    /// <summary>The ids of the views on the canvas, in their order on screen.</summary>
    protected Task<string[]> PaneIds() => Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

    /// <summary>Waits until the view has settled at this size, as its own page sees it.</summary>
    protected async Task ExpectViewSize(int width, int height, string paneId = "1")
    {
        await WaitForViews();

        var size = (Width: 0, Height: 0);

        try
        {
            await WorkbenchTestReviewer.WaitFor(async () => size = await ViewSize(paneId), read => read == (width, height));
        }
        catch (TimeoutException)
        {
            Assert.AreEqual((width, height), size);
        }
    }

    /// <summary>The view's window size, or none while its frame is between two documents.</summary>
    protected async Task<(int Width, int Height)> ViewSize(string paneId = "1")
    {
        try
        {
            var size = await View(paneId).Locator("html").EvaluateAsync<int[]>("() => [window.innerWidth, window.innerHeight]");

            return (size[0], size[1]);
        }
        catch (PlaywrightException)
        {
            return (0, 0);
        }
    }

    /// <summary>Everything the browser saved for the shell and the views.</summary>
    protected Task<string> SavedState() => Page.EvaluateAsync<string>("""
        () => new Promise(resolve => {
            const open = indexedDB.open('bit-workbench');
            open.onerror = () => resolve('');
            open.onsuccess = () => {
                const database = open.result;
                if (database.objectStoreNames.contains('state') === false) return resolve('');
                const all = database.transaction('state').objectStore('state').getAll();
                all.onerror = () => resolve('');
                all.onsuccess = () => resolve(JSON.stringify(all.result));
            };
        })
        """);

    /// <summary>Drags the mouse from the middle of an element by this distance on the screen.</summary>
    protected async Task DragBy(ILocator element, float x, float y)
    {
        var box = (await element.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + x, box.Y + box.Height / 2 + y, new() { Steps = 5 });
        await Page.Mouse.UpAsync();
    }

    /// <summary>Moves a finger from one spot of the screen to another at a finger's pace, which Playwright's own touch can't do.</summary>
    protected async Task DragWithAFinger((double X, double Y) from, (double X, double Y) to)
    {
        var cdp = await Context.NewCDPSessionAsync(Page);

        Task Touch(string type, params (double X, double Y)[] points) => cdp.SendAsync("Input.dispatchTouchEvent", new()
        {
            ["type"] = type,
            ["touchPoints"] = points.Select(point => new Dictionary<string, object> { ["x"] = point.X, ["y"] = point.Y }).ToArray()
        });

        await Touch("touchStart", from);

        foreach (var step in Enumerable.Range(1, 10))
        {
            await Touch("touchMove", (from.X + (to.X - from.X) * step / 10, from.Y + (to.Y - from.Y) * step / 10));
            await Page.WaitForTimeoutAsync(16);
        }

        // It stops before it lifts, as the browser takes a finger that lifts on the move for a flick and ignores the next tap.
        await Page.WaitForTimeoutAsync(150);
        await Touch("touchEnd");
    }
}
