using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Folders dropped on a target with <c>ExpandDirectories</c>. A drag a script makes carries no folders, so the page's
/// <c>DataTransferItem.webkitGetAsEntry</c> is made to answer with a tree of entries for the placeholder files named
/// after the folders - the browser's own file system entry API, as the module walks it, with batches, nested folders,
/// an empty folder and an entry that cannot be read.
/// </summary>
[TestClass]
public sealed class FolderDropTests : BdndTest
{
    // Entries behave as the browser's do: readEntries hands them out in batches of two, then an empty batch; file()
    // answers asynchronously; an unreadable file fails.
    private const string FakeEntries =
        """
        (tree) => {
          const file = (path, content, type, unreadable) => ({
            isFile: true, isDirectory: false, name: path.split('/').pop(), fullPath: '/' + path,
            file: (ok, fail) => setTimeout(() => unreadable
              ? fail(new DOMException('cannot read', 'NotReadableError'))
              : ok(new File([content], path.split('/').pop(), { type })))
          });
          const folder = (path, children) => ({
            isFile: false, isDirectory: true, name: path.split('/').pop(), fullPath: '/' + path,
            createReader: () => {
              let next = 0;
              return { readEntries: (ok) => setTimeout(() => { ok(children.slice(next, next + 2)); next += 2; }) };
            }
          });
          const build = (node, path) => node.children
            ? folder(path, Object.entries(node.children).map(([name, child]) => build(child, path + '/' + name)))
            : file(path, node.content ?? '', node.type ?? '', node.unreadable);
          const roots = {};
          for (const [name, node] of Object.entries(tree)) roots[name] = build(node, name);
          const real = DataTransferItem.prototype.webkitGetAsEntry;
          DataTransferItem.prototype.webkitGetAsEntry = function () {
            const f = this.getAsFile();
            return (f && roots[f.name]) || (real ? real.call(this) : null);
          };
        }
        """;

    private static ILocator Rows(IPage page) => page.Locator(".demo-table tbody tr");

    private async Task DropAsync(IPage page, object tree, string fill)
    {
        await page.EvaluateAsync(FakeEntries, tree);
        var bin = page.Locator(".dustbin");
        var files = await NativeDataTransferAsync(page, fill);
        await DispatchDragAsync(bin, "dragenter", files);
        await DispatchDragAsync(bin, "dragover", files);
        await DispatchDragAsync(bin, "drop", files);
    }

    [TestMethod]
    public async Task A_dropped_folder_is_walked_into_its_files_with_their_paths_and_contents()
    {
        var page = await OpenAsync("/other/native-files");
        var tree = new Dictionary<string, object>
        {
            ["photos"] = new
            {
                children = new Dictionary<string, object>
                {
                    ["a.txt"] = new { content = "alpha", type = "text/plain" },
                    ["raw"] = new { children = new Dictionary<string, object> { ["b.txt"] = new { content = "bravo", type = "text/plain" } } },
                    ["empty"] = new { children = new Dictionary<string, object>() },
                    ["c.txt"] = new { content = "charlie", type = "text/plain" },
                },
            },
        };

        // the folder, as a browser hands it over: a nameless-typed placeholder file - and a loose file beside it
        await DropAsync(page, tree, """
            dt.items.add(new File([], 'photos'));
            dt.items.add(new File(['note'], 'note.txt', { type: 'text/plain' }))
            """);

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dropped files (4)" })).ToBeVisibleAsync();
        await Expect(Rows(page).Nth(0).Locator("td")).ToHaveTextAsync(["photos/a.txt", "text/plain", "5 B", "alpha"]);
        await Expect(Rows(page).Nth(1).Locator("td")).ToHaveTextAsync(["photos/raw/b.txt", "text/plain", "5 B", "bravo"]);
        await Expect(Rows(page).Nth(2).Locator("td")).ToHaveTextAsync(["photos/c.txt", "text/plain", "7 B", "charlie"]);
        await Expect(Rows(page).Nth(3).Locator("td")).ToHaveTextAsync(["note.txt", "text/plain", "4 B", "note"]);
        await Expect(page.Locator(".native-incomplete")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task An_unreadable_entry_is_skipped_and_marks_the_list_incomplete()
    {
        var page = await OpenAsync("/other/native-files");
        var tree = new Dictionary<string, object>
        {
            ["docs"] = new
            {
                children = new Dictionary<string, object>
                {
                    ["locked.txt"] = new { content = "secret", type = "text/plain", unreadable = true },
                    ["open.txt"] = new { content = "open", type = "text/plain" },
                },
            },
        };

        await DropAsync(page, tree, "dt.items.add(new File([], 'docs'))");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dropped files (1)" })).ToBeVisibleAsync();
        await Expect(Rows(page).Nth(0).Locator("td")).ToHaveTextAsync(["docs/open.txt", "text/plain", "4 B", "open"]);
        await Expect(page.Locator(".native-incomplete")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_walk_stops_at_the_targets_file_limit()
    {
        var page = await OpenAsync("/other/native-files");
        var many = Enumerable.Range(1, 130).ToDictionary(i => $"f{i:000}.txt", i => (object)new { content = $"{i}", type = "text/plain" });
        var tree = new Dictionary<string, object> { ["big"] = new { children = many } };

        await DropAsync(page, tree, "dt.items.add(new File([], 'big'))");

        // MaxFiles is 100 on the page: the walk stops there, and says the list is not all of it
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dropped files (100)" })).ToBeVisibleAsync();
        await Expect(Rows(page)).ToHaveCountAsync(100);
        await Expect(Rows(page).Nth(99).Locator("td").First).ToHaveTextAsync("big/f100.txt");
        await Expect(page.Locator(".native-incomplete")).ToBeVisibleAsync();
    }
}
