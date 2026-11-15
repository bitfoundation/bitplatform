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
/// What a server grid (OnRead) shows while it reads, and what a read that fails leaves when the app handles that
/// (OnReadError): an error row with a Retry button in place of the rows.
/// </summary>
[TestClass]
public class BitDataGridServerReadTests : BunitTestContext
{
    public class TestRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static List<TestRow> CreateRows(int count, int from = 1)
        => Enumerable.Range(from, count).Select(i => new TestRow { Id = i, Name = $"row-{i}" }).ToList();

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<BitDataGridColumn<TestRow>>(0);
        builder.AddComponentParameter(1, "Field", "Name");
        builder.CloseComponent();
    };

    // A source whose reads stay out until the test answers them.
    private sealed class PendingReads
    {
        public List<(BitDataGridReadRequest Request, TaskCompletionSource<BitDataGridReadResult<TestRow>> Answer)> All { get; } = new();

        public Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>> OnRead => request =>
        {
            var answer = new TaskCompletionSource<BitDataGridReadResult<TestRow>>();
            All.Add((request, answer));
            return answer.Task;
        };
    }

    // A source answering every read at once, failing those `fails` picks.
    private static Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TestRow>>> Reader(
        List<TestRow> all, List<BitDataGridReadRequest> requests, Func<BitDataGridReadRequest, bool> fails)
        => request =>
        {
            requests.Add(request);
            if (fails(request)) return Task.FromException<BitDataGridReadResult<TestRow>>(new InvalidOperationException("The server is down."));

            IEnumerable<TestRow> rows = all.Skip(request.Skip);
            if (request.Take is { } take) rows = rows.Take(take);
            return Task.FromResult(new BitDataGridReadResult<TestRow>(rows.ToList(), all.Count));
        };

    private static IElement Table(IRenderedComponent<BitDataGrid<TestRow>> component) => component.Find(".bit-dtg-table");

    private static IReadOnlyList<IElement> DataRows(IRenderedComponent<BitDataGrid<TestRow>> component)
        => component.FindAll(".bit-dtg-body .bit-dtg-row:not(.bit-dtg-message-row)");

    [TestMethod]
    [DataRow(3, DisplayName = "Rows come back")]
    [DataRow(0, DisplayName = "No rows come back")]
    public void BitDataGridShouldSayItIsLoadingWhileItReadsItsFirstRows(int count)
    {
        var reads = new PendingReads();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, reads.OnRead);
            parameters.Add(p => p.ChildContent, Columns());
        });

        // Until the first rows are in, the grid does not know there are none: it says it is loading them.
        Assert.HasCount(1, reads.All);
        Assert.HasCount(1, component.FindAll(".bit-dtg-loading"));
        Assert.IsEmpty(component.FindAll(".bit-dtg-empty"));
        Assert.AreEqual("true", Table(component).GetAttribute("aria-busy"));

        reads.All[0].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(count), count));

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-dtg-loading"));
            Assert.HasCount(count, DataRows(component));
            Assert.HasCount(count == 0 ? 1 : 0, component.FindAll(".bit-dtg-empty"));
            Assert.IsNull(Table(component).GetAttribute("aria-busy"));
        });
    }

    [TestMethod]
    public async Task BitDataGridShouldKeepItsRowsUpWhileItReadsTheNextOnes()
    {
        var reads = new PendingReads();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, reads.OnRead);
            parameters.Add(p => p.ChildContent, Columns());
        });
        reads.All[0].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(3), 3));
        component.WaitForAssertion(() => Assert.HasCount(3, DataRows(component)));

        Task sort = Task.CompletedTask;
        await component.InvokeAsync(() => { sort = component.Find("button.bit-dtg-htext").ClickAsync(new MouseEventArgs()); });

        // The rows on screen stay up rather than giving way to a loading row; only the grid says it is busy.
        Assert.HasCount(2, reads.All);
        Assert.HasCount(3, DataRows(component));
        Assert.IsEmpty(component.FindAll(".bit-dtg-loading"));
        Assert.AreEqual("true", Table(component).GetAttribute("aria-busy"));

        reads.All[1].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(3).AsEnumerable().Reverse().ToList(), 3));
        await sort;

        component.WaitForAssertion(() =>
        {
            Assert.IsNull(Table(component).GetAttribute("aria-busy"));
            Assert.AreEqual("row-3", DataRows(component)[0].TextContent.Trim());
        });
    }

    [TestMethod]
    public void BitDataGridShouldLetAFailedReadThrowWhenTheAppDoesNotHandleIt()
    {
        var requests = new List<BitDataGridReadRequest>();

        Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(3), requests, _ => true));
            parameters.Add(p => p.ChildContent, Columns());
        }));
    }

    [TestMethod]
    public void BitDataGridShouldShowTheErrorRowInPlaceOfTheRowsWhenAReadFails()
    {
        var errors = new List<Exception>();
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(3), requests, _ => true));
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });

        Assert.HasCount(1, errors);
        Assert.AreEqual("The server is down.", errors[0].Message);

        var cell = component.Find(".bit-dtg-message-row .bit-dtg-read-error");
        StringAssert.Contains(cell.TextContent, "The records could not be loaded.");
        var retry = cell.QuerySelector("button.bit-dtg-retry")!;
        Assert.AreEqual("Retry", retry.TextContent.Trim());
        Assert.IsNull(retry.GetAttribute("aria-disabled"));
        Assert.IsFalse(retry.HasAttribute("disabled"));

        Assert.IsEmpty(DataRows(component));
        Assert.IsEmpty(component.FindAll(".bit-dtg-empty"));
        Assert.IsEmpty(component.FindAll(".bit-dtg-loading"));
        Assert.IsNull(Table(component).GetAttribute("aria-busy"));

        // The header row and the error row, the one row a body with no data row renders.
        Assert.AreEqual("2", Table(component).GetAttribute("aria-rowcount"));

        // The failure is told to screen readers too: nothing else announces that the rows never came.
        StringAssert.Contains(component.Find("[role=status]").TextContent, "The records could not be loaded.");
    }

    [TestMethod]
    [DataRow(true, DisplayName = "The read succeeds")]
    [DataRow(false, DisplayName = "The read fails again")]
    public async Task BitDataGridShouldReadTheRowsAgainOnRetry(bool succeeds)
    {
        var errors = new List<Exception>();
        var reads = new PendingReads();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, reads.OnRead);
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });
        reads.All[0].Answer.SetException(new InvalidOperationException("The server is down."));
        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll(".bit-dtg-read-error")));

        Task retry = Task.CompletedTask;
        await component.InvokeAsync(() => { retry = component.Find("button.bit-dtg-retry").ClickAsync(new MouseEventArgs()); });

        // While the rows are read again the error row stays, saying so, with its button marked busy rather than
        // disabled - so the focus on it stays too - and a second press reads nothing more.
        Assert.HasCount(2, reads.All);
        var cell = component.Find(".bit-dtg-read-error");
        StringAssert.Contains(cell.TextContent, "Loading…");
        Assert.HasCount(1, cell.QuerySelectorAll(".bit-dtg-spinner"));
        var button = component.Find("button.bit-dtg-retry");
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-busy"));
        Assert.IsFalse(button.HasAttribute("disabled"));
        Assert.AreEqual("true", Table(component).GetAttribute("aria-busy"));

        await component.InvokeAsync(() => { _ = component.Find("button.bit-dtg-retry").ClickAsync(new MouseEventArgs()); });
        Assert.HasCount(2, reads.All);

        if (succeeds)
        {
            reads.All[1].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(3), 3));
        }
        else
        {
            reads.All[1].Answer.SetException(new InvalidOperationException("Still down."));
        }
        await retry;

        component.WaitForAssertion(() =>
        {
            Assert.IsNull(Table(component).GetAttribute("aria-busy"));
            if (succeeds)
            {
                Assert.IsEmpty(component.FindAll(".bit-dtg-read-error"));
                Assert.HasCount(3, DataRows(component));
                Assert.HasCount(1, errors);
            }
            else
            {
                StringAssert.Contains(component.Find(".bit-dtg-read-error").TextContent, "The records could not be loaded.");
                Assert.IsNull(component.Find("button.bit-dtg-retry").GetAttribute("aria-disabled"));
                Assert.IsEmpty(DataRows(component));
                Assert.HasCount(2, errors);
                Assert.AreEqual("Still down.", errors[1].Message);
            }
        });
    }

    // The Retry button goes with the error row once the rows are back, so the focus it held goes to their first cell -
    // where a cell can take it: in a grid whose cells take no focus, there is no cell to give it to.
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitDataGridShouldMoveTheFocusToTheFirstCellWhenARetryBringsTheRowsBack(bool cellNavigation)
    {
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(3), requests, _ => requests.Count == 1));
            parameters.Add(p => p.OnReadError, (Exception _) => { });
            parameters.Add(p => p.CellNavigation, cellNavigation);
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.Find("button.bit-dtg-retry").Click();

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(3, DataRows(component));
            var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
            if (cellNavigation)
            {
                var first = DataRows(component)[0].QuerySelector(".bit-dtg-cell")!;
                Assert.AreEqual("0", first.GetAttribute("tabindex"));
                Assert.HasCount(1, focus);
                Assert.AreEqual(first.GetAttribute("blazor:elementReference"), ((ElementReference)focus[0].Arguments[0]!).Id);
            }
            else
            {
                Assert.IsEmpty(focus);
            }
        });
    }

    // A failed page leaves the total read before, so the pager still pages away from it.
    [TestMethod]
    public void BitDataGridShouldKeepThePagerOfAFailedPage()
    {
        var errors = new List<Exception>();
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(5), requests, r => r.Skip == 2));
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.Pageable, true);
            parameters.Add(p => p.PageSize, 2);
            parameters.Add(p => p.ChildContent, Columns());
        });
        Assert.HasCount(2, DataRows(component));

        component.Find("button[aria-label='Next page']").Click();

        Assert.HasCount(1, errors);
        Assert.HasCount(1, component.FindAll(".bit-dtg-read-error"));
        Assert.IsEmpty(DataRows(component));
        StringAssert.Contains(component.Find(".bit-dtg-pager-info").TextContent, "of 5");

        component.Find("button[aria-label='Next page']").Click();

        Assert.AreEqual(4, requests[^1].Skip);
        Assert.IsEmpty(component.FindAll(".bit-dtg-read-error"));
        Assert.AreEqual("row-5", DataRows(component).Single().TextContent.Trim());
    }

    // A read that a newer one replaced, failing late, is no news: the newer read tells how it went.
    [TestMethod]
    public async Task BitDataGridShouldNotReportAFailedReadThatANewerOneReplaced()
    {
        var errors = new List<Exception>();
        var reads = new PendingReads();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, reads.OnRead);
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });
        reads.All[0].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(3), 3));
        component.WaitForAssertion(() => Assert.HasCount(3, DataRows(component)));

        Task first = Task.CompletedTask, second = Task.CompletedTask;
        await component.InvokeAsync(() => { first = component.Find("button.bit-dtg-htext").ClickAsync(new MouseEventArgs()); });
        await component.InvokeAsync(() => { second = component.Find("button.bit-dtg-htext").ClickAsync(new MouseEventArgs()); });
        Assert.HasCount(3, reads.All);

        // An OnRead that ignores the cancellation of its token fails with an error of its own.
        reads.All[1].Answer.SetException(new InvalidOperationException("Late."));
        await first;

        Assert.IsEmpty(errors);
        Assert.IsEmpty(component.FindAll(".bit-dtg-read-error"));
        Assert.AreEqual("true", Table(component).GetAttribute("aria-busy"));

        reads.All[2].Answer.SetResult(new BitDataGridReadResult<TestRow>(CreateRows(3), 3));
        await second;

        component.WaitForAssertion(() =>
        {
            Assert.IsNull(Table(component).GetAttribute("aria-busy"));
            Assert.HasCount(3, DataRows(component));
        });
        Assert.IsEmpty(errors);
    }

    // A read that fails once the grid is gone has no rows left to stand in for, and no one to tell.
    [TestMethod]
    public async Task BitDataGridShouldNotReportAReadThatFailsAfterItIsDisposed()
    {
        var errors = new List<Exception>();
        var reads = new PendingReads();
        RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, reads.OnRead);
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });

        await Context.DisposeComponentsAsync();
        reads.All[0].Answer.SetException(new InvalidOperationException("Too late."));
        await Task.Delay(200);

        Assert.IsEmpty(errors);
    }

    // A server-virtualized grid takes Virtualize out of the page for the error row, and Retry reads the first window
    // ahead of a new one, the way the grid's first render does.
    [TestMethod]
    public void BitDataGridShouldShowTheErrorRowInPlaceOfAVirtualizedGridsFirstRows()
    {
        var errors = new List<Exception>();
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.OnRead, Reader(CreateRows(10), requests, _ => requests.Count == 1));
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(1, errors);
            Assert.HasCount(1, component.FindAll(".bit-dtg-read-error"));
            Assert.IsEmpty(component.FindAll(".bit-dtg-placeholder-row"));
            Assert.IsEmpty(DataRows(component));
        });
        Assert.HasCount(1, requests);

        component.Find("button.bit-dtg-retry").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-dtg-read-error"));
            Assert.HasCount(10, DataRows(component));
            Assert.AreEqual(0, requests[^1].Skip);
        });
        Assert.HasCount(1, errors);
    }

    // A window Virtualize asks for as it scrolls failing is the grid's to handle too.
    [TestMethod]
    public void BitDataGridShouldShowTheErrorRowWhenAWindowOfAVirtualizedGridFails()
    {
        var errors = new List<Exception>();
        var requests = new List<BitDataGridReadRequest>();
        var fail = true;
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.Virtualize, true);
            // The first window is read ahead of Virtualize; the rows past it are read when it asks for more.
            parameters.Add(p => p.OnRead, Reader(CreateRows(1000), requests, r => fail && r.Skip > 0));
            parameters.Add(p => p.OnReadError, (Exception e) => errors.Add(e));
            parameters.Add(p => p.ChildContent, Columns());
        });

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(1, errors);
            Assert.HasCount(1, component.FindAll(".bit-dtg-read-error"));
            Assert.IsEmpty(DataRows(component));
        });

        fail = false;
        component.Find("button.bit-dtg-retry").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-dtg-read-error"));
            Assert.IsNotEmpty(DataRows(component));
        });
        Assert.HasCount(1, errors);
    }

    [TestMethod]
    public void BitDataGridShouldNotRetryInADisabledGrid()
    {
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(3), requests, _ => true));
            parameters.Add(p => p.OnReadError, (Exception _) => { });
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.ChildContent, Columns());
        });

        var retry = component.Find("button.bit-dtg-retry");
        Assert.IsTrue(retry.HasAttribute("disabled"));
        Assert.AreEqual("true", retry.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitDataGridShouldDressTheErrorRowFromClassesStylesAndStrings()
    {
        var requests = new List<BitDataGridReadRequest>();
        var component = RenderComponent<BitDataGrid<TestRow>>(parameters =>
        {
            parameters.Add(p => p.OnRead, Reader(CreateRows(3), requests, _ => true));
            parameters.Add(p => p.OnReadError, (Exception _) => { });
            parameters.Add(p => p.Classes, new BitDataGridClassStyles { ReadError = "my-error" });
            parameters.Add(p => p.Styles, new BitDataGridClassStyles { ReadError = "color: red;" });
            parameters.Add(p => p.Strings, new BitDataGridStrings { ReadErrorText = "Kein Zugriff.", RetryText = "Nochmal" });
            parameters.Add(p => p.ChildContent, Columns());
        });

        var cell = component.Find(".bit-dtg-read-error");
        Assert.IsTrue(cell.ClassList.Contains("my-error"));
        StringAssert.Contains(cell.GetAttribute("style"), "color: red;");
        StringAssert.Contains(cell.TextContent, "Kein Zugriff.");
        Assert.AreEqual("Nochmal", component.Find("button.bit-dtg-retry").TextContent.Trim());
    }
}
