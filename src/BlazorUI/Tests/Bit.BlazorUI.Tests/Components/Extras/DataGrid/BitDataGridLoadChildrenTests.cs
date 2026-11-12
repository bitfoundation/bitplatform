using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.DataGrid;

/// <summary>
/// A lazy tree loaded through OnLoadChildren: the node and a token reach the app, and the grid cancels that token once
/// it no longer needs the children - when new Items replace the hierarchy, or when the grid is disposed - and only then.
/// </summary>
[TestClass]
public class BitDataGridLoadChildrenTests : BunitTestContext
{
    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
    }

    private static List<Node> Roots(params string[] names)
        => names.Select((name, i) => new Node { Id = i + 1, Name = name, IsFolder = true }).ToList();

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<Node>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();
    };

    // Children that stay out until the test answers them, fetched the way a call handed the token is: it stops,
    // throwing, once that token is cancelled.
    private sealed class PendingChildren
    {
        public List<(BitDataGridChildrenRequest<Node> Request, TaskCompletionSource<IEnumerable<Node>?> Answer)> All { get; } = new();

        public Func<BitDataGridChildrenRequest<Node>, Task<IEnumerable<Node>?>> OnLoadChildren => request =>
        {
            var answer = new TaskCompletionSource<IEnumerable<Node>?>();
            request.CancellationToken.Register(() => answer.TrySetCanceled(request.CancellationToken));
            All.Add((request, answer));
            return answer.Task;
        };
    }

    private IRenderedComponent<BitDataGrid<Node>> RenderLazyTree(
        List<Node> roots, Func<BitDataGridChildrenRequest<Node>, Task<IEnumerable<Node>?>> onLoadChildren, bool cellNavigation = false)
        => RenderComponent<BitDataGrid<Node>>(parameters =>
        {
            parameters.Add(p => p.Items, roots);
            parameters.Add(p => p.KeyField, (Func<Node, object>)(n => n.Id));
            parameters.Add(p => p.HasChildrenSelector, (Func<Node, bool>)(n => n.IsFolder));
            parameters.Add(p => p.OnLoadChildren, onLoadChildren);
            parameters.Add(p => p.CellNavigation, cellNavigation);
            parameters.Add(p => p.ChildContent, Columns());
        });

    private static IElement Toggle(IRenderedComponent<BitDataGrid<Node>> component, int row = 0)
        => component.FindAll(".bit-dtg-tree-toggle")[row];

    private static IReadOnlyList<string> RowTexts(IRenderedComponent<BitDataGrid<Node>> component)
        => component.FindAll(".bit-dtg-body > .bit-dtg-row:not(.bit-dtg-message-row)")
            .Select(r => r.QuerySelector(".bit-dtg-cell-text")!.TextContent.Trim())
            .ToList();

    // Each click is started on the renderer's dispatcher and its task kept rather than awaited there (a statement
    // lambda, not an expression one, which would hand InvokeAsync the task to await): the handler waits on a fetch
    // this test only answers further down.
    private static async Task<Task> ClickAsync(IRenderedComponent<BitDataGrid<Node>> component, IElement toggle)
    {
        Task click = Task.CompletedTask;
        await component.InvokeAsync(() => { click = toggle.ClickAsync(new MouseEventArgs()); });
        return click;
    }

    [TestMethod]
    public async Task BitDataGridShouldLoadALazyTreesChildrenThroughOnLoadChildren()
    {
        var children = new PendingChildren();
        var roots = Roots("root");
        var component = RenderLazyTree(roots, children.OnLoadChildren);

        // HasChildrenSelector gives the unloaded root its toggle, as it does for ChildrenProvider.
        var expand = await ClickAsync(component, Toggle(component));
        Assert.HasCount(1, children.All);
        Assert.AreSame(roots[0], children.All[0].Request.Parent);
        Assert.IsFalse(children.All[0].Request.CancellationToken.IsCancellationRequested);

        children.All[0].Answer.SetResult([new Node { Id = 11, Name = "child" }]);
        await expand;
        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "root", "child" }, RowTexts(component).ToArray()));

        // The children are kept: neither a toggle nor expanding everything asks for them again.
        Toggle(component).Click();
        Toggle(component).Click();
        await component.InvokeAsync(() => component.Instance.CollapseAllAsync());
        await component.InvokeAsync(() => component.Instance.ExpandAllAsync());
        CollectionAssert.AreEqual(new[] { "root", "child" }, RowTexts(component).ToArray());
        Assert.HasCount(1, children.All);
    }

    [TestMethod]
    public async Task BitDataGridShouldCancelAChildrenFetchWhenNewItemsReplaceTheHierarchy()
    {
        var children = new PendingChildren();
        var component = RenderLazyTree(Roots("root"), children.OnLoadChildren, cellNavigation: true);

        // The forward arrow opens the root from its cell.
        var cell = component.Find(".bit-dtg-body > .bit-dtg-row [aria-colindex='1']");
        cell.FocusIn();
        Task press = Task.CompletedTask;
        await component.InvokeAsync(() => { press = cell.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" }); });
        Assert.HasCount(1, children.All);
        var oldToken = children.All[0].Request.CancellationToken;
        var focusCalls = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        // The app reloads its rows (a new list, the same ids) while the children are on their way.
        component.Render(p => p.Add(x => x.Items, Roots("root")));
        Assert.IsTrue(oldToken.IsCancellationRequested, "nothing the old fetch brings back is kept, so it can stop");

        // A fetch the grid cancelled itself is not a failure: the key press goes on as after any expand, and puts
        // the focus back on the cell it was pressed in.
        await press;
        component.WaitForAssertion(() =>
            Assert.HasCount(focusCalls + 1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]));

        // The reloaded root fetches its own children, with a token the old hierarchy's end did not cancel.
        var newExpand = await ClickAsync(component, Toggle(component));
        Assert.HasCount(2, children.All);
        Assert.IsFalse(children.All[1].Request.CancellationToken.IsCancellationRequested);
        children.All[1].Answer.SetResult([new Node { Id = 11, Name = "fresh-child" }]);
        await newExpand;

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "root", "fresh-child" }, RowTexts(component).ToArray()));
    }

    [TestMethod]
    public async Task BitDataGridShouldCancelAChildrenFetchWhenItIsDisposed()
    {
        var children = new PendingChildren();
        var component = RenderLazyTree(Roots("root"), children.OnLoadChildren);

        var expand = await ClickAsync(component, Toggle(component));
        await Context.DisposeComponentsAsync();

        Assert.IsTrue(children.All[0].Request.CancellationToken.IsCancellationRequested);
        await expand;
    }

    // Only the end of the hierarchy or of the grid cancels a fetch: rendering the same rows again, another node's fetch
    // or collapsing everything does not, and the children still land in the cache.
    [TestMethod]
    public async Task BitDataGridShouldNotCancelAChildrenFetchForAnythingElse()
    {
        var children = new PendingChildren();
        var roots = Roots("first", "second");
        var component = RenderLazyTree(roots, children.OnLoadChildren);

        var firstExpand = await ClickAsync(component, Toggle(component, 0));
        var secondExpand = await ClickAsync(component, Toggle(component, 1));
        component.Render(p => p.Add(x => x.Items, roots));
        await component.InvokeAsync(() => component.Instance.CollapseAllAsync());

        Assert.HasCount(2, children.All);
        Assert.IsFalse(children.All[0].Request.CancellationToken.IsCancellationRequested);
        Assert.IsFalse(children.All[1].Request.CancellationToken.IsCancellationRequested);

        children.All[0].Answer.SetResult([new Node { Id = 11, Name = "first-child" }]);
        children.All[1].Answer.SetResult([new Node { Id = 21, Name = "second-child" }]);
        await firstExpand;
        await secondExpand;

        await component.InvokeAsync(() => component.Instance.ExpandAllAsync());
        CollectionAssert.AreEqual(new[] { "first", "first-child", "second", "second-child" }, RowTexts(component).ToArray());
        Assert.HasCount(2, children.All);
    }

    // A fetch that stops for reasons of its own (a timeout, say) failed: the grid did not ask it to stop. The node
    // closes again, as after any failed fetch, so the next expand asks again.
    [TestMethod]
    public void BitDataGridShouldStillFailAnExpandWhoseFetchCancelsItself()
    {
        var calls = 0;
        var component = RenderLazyTree(Roots("root"), _ =>
        {
            calls++;
            return Task.FromException<IEnumerable<Node>?>(new TaskCanceledException("The request timed out."));
        });

        Toggle(component).Click();
        Assert.AreEqual("false", Toggle(component).GetAttribute("aria-expanded"));

        Toggle(component).Click();
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    [DataRow(nameof(BitDataGrid<Node>.ChildrenSelector))]
    [DataRow(nameof(BitDataGrid<Node>.ChildrenProvider))]
    [DataRow(nameof(BitDataGrid<Node>.OnRead))]
    public void BitDataGridShouldNotTakeOnLoadChildrenBesideAnotherSourceOfRows(string other)
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<BitDataGrid<Node>>(parameters =>
        {
            parameters.Add(p => p.Items, Roots("root"));
            parameters.Add(p => p.OnLoadChildren, new PendingChildren().OnLoadChildren);
            if (other == nameof(BitDataGrid<Node>.ChildrenSelector))
                parameters.Add(p => p.ChildrenSelector, (Func<Node, IEnumerable<Node>?>)(_ => null));
            else if (other == nameof(BitDataGrid<Node>.ChildrenProvider))
                parameters.Add(p => p.ChildrenProvider, (Func<Node, Task<IEnumerable<Node>?>>)(_ => Task.FromResult<IEnumerable<Node>?>(null)));
            else
                parameters.Add(p => p.OnRead, (Func<BitDataGridReadRequest, Task<BitDataGridReadResult<Node>>>)(_ =>
                    Task.FromResult(new BitDataGridReadResult<Node>([], 0))));
            parameters.Add(p => p.ChildContent, Columns());
        }));

        StringAssert.Contains(exception.Message, nameof(BitDataGrid<Node>.OnLoadChildren));
        StringAssert.Contains(exception.Message, other);
    }
}
