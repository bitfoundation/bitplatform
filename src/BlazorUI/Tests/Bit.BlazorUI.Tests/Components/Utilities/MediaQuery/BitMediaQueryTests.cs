using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.MediaQuery;

[TestClass]
public class BitMediaQueryTests : BunitTestContext
{
    // The positions of the arguments the component invokes BitBlazorUI.MediaQuery.setup with.
    private const int KeyArg = 0;
    private const int ElementIdArg = 1;
    private const int QueryArg = 2;
    private const int ScreenQueryArg = 3;
    private const int BreakpointsArg = 4;

    private static RenderFragment Markup(string markup) => builder => builder.AddMarkupContent(0, markup);

    private static List<JSRuntimeInvocation> Setups(BunitJSInterop jsInterop)
        => jsInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.setup").ToList();

    private static List<JSRuntimeInvocation> Disposals(BunitJSInterop jsInterop)
        => jsInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.MediaQuery.dispose").ToList();



    [TestMethod]
    public void BitMediaQueryShouldRenderNothingWithoutAnyContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.OnChange, (bool _) => { });
        });

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderRootElementWhenContentProvided()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.IsNotNull(root);
        Assert.IsFalse(string.IsNullOrEmpty(root.Id));
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderNotMatchedContentInitially()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderMatchedContentInitiallyWithDefaultMatched()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);
        Assert.IsTrue(component.Instance.IsMatched);
    }

    [TestMethod]
    public void BitMediaQueryShouldSwitchContentOnMatchChange()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderChildContentAsMatchedContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<div class=\"child\">Child</div>");
        });

        Assert.AreEqual(0, component.FindAll(".child").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldPreferMatchedOverChildContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.AddChildContent("<div class=\"child\">Child</div>");
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderNothingWhenTheActiveSideIsAbsent()
    {
        // Only one of the two sides is provided, which is how a piece of markup is dropped on the
        // other state of the query: the element is still rendered, and it is simply empty.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
        });

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(string.Empty, component.Find(".bit-mdq").InnerHtml.Trim());

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".matched").Count);
    }



    [TestMethod]
    public void BitMediaQueryShouldRenderTemplateWithTheMatchedState()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Template, (RenderFragment<bool>)(matched => builder =>
                builder.AddMarkupContent(0, $"<div class=\"tpl\">{matched}</div>")));
        });

        Assert.AreEqual("False", component.Find(".tpl").TextContent);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual("True", component.Find(".tpl").TextContent);
    }

    [TestMethod]
    public void BitMediaQueryShouldPreferTemplateOverEveryOtherContent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Template, (RenderFragment<bool>)(_ => builder =>
                builder.AddMarkupContent(0, "<div class=\"tpl\">Template</div>")));
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
            parameters.AddChildContent("<div class=\"child\">Child</div>");
        });

        Assert.AreEqual(1, component.FindAll(".tpl").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".tpl").Count);
        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderTemplateWithoutTheWrapperInNoWrapperMode()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Template, (RenderFragment<bool>)(matched => builder =>
                builder.AddMarkupContent(0, $"<div class=\"tpl\">{matched}</div>")));
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual("False", component.Find(".tpl").TextContent);
    }

    [TestMethod]
    public void BitMediaQueryShouldSetUpTheListenerForATemplateOnlyUsage()
    {
        // A Template is content like any other, so it is what makes the component render an element
        // of its own - and that element is the scope the theme breakpoints are read from.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.Template, (RenderFragment<bool>)(_ => builder =>
                builder.AddMarkupContent(0, "<div class=\"tpl\">Template</div>")));
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[ElementIdArg]);
    }



    [TestMethod]
    public void BitMediaQueryShouldInvokeOnChangeWhenJsNotifies()
    {
        bool? changed = null;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.OnChange, (bool v) => changed = v);
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();
        Assert.IsTrue(changed);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();
        Assert.IsFalse(changed);
    }

    [TestMethod]
    public void BitMediaQueryShouldInvokeOnChangeForTheInitialResultEvenWhenItMatchesTheCurrentState()
    {
        // The first notification carries the initial result of the query, which a DefaultMatched may
        // well have guessed right. A handler waiting for the first real answer still has to hear it.
        var reported = new List<bool>();
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.Add(p => p.OnChange, (bool v) => reported.Add(v));
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        CollectionAssert.AreEqual(new[] { true }, reported);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotRenderAgainWhenTheReportedStateIsUnchanged()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.AddChildContent("<span>content</span>");
        });

        var renderCount = component.RenderCount;

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(renderCount, component.RenderCount);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();

        Assert.IsTrue(component.RenderCount > renderCount);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotReportAThrowingHandlerBackOverTheInteropCall()
    {
        // The JS side reads a rejected notification as "the .NET object is gone" and stops listening
        // for good, so a handler of the page throwing once must not travel back that way - it goes to
        // Blazor's own error handling instead, which is what an error boundary is there to catch.
        Exception? captured = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => captured = ex));
            parameters.AddChildContent<BitMediaQuery>(mediaQuery =>
            {
                mediaQuery.Add(p => p.Query, "(max-width: 600px)");
                mediaQuery.Add(p => p.OnChange, (bool _) => throw new InvalidOperationException("boom"));
            });
        });

        var mediaQuery = component.FindComponent<BitMediaQuery>().Instance;

        // The notification itself completes: nothing is thrown back at the caller on the JS side.
        component.InvokeAsync(() => mediaQuery._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        // ... and the error is where the framework puts an unhandled one, rather than swallowed.
        Assert.IsInstanceOfType<InvalidOperationException>(captured);
    }

    [TestMethod]
    public void BitMediaQueryShouldExposeIsMatchedState()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.IsFalse(component.Instance.IsMatched);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.IsTrue(component.Instance.IsMatched);
    }

    [TestMethod]
    public void BitMediaQueryShouldWriteBackTheMatchedStateToItsBinding()
    {
        var bound = false;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Bind(p => p.IsMatched, bound, v => bound = v);
            parameters.AddChildContent("<span>content</span>");
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();
        Assert.IsTrue(bound);

        component.InvokeAsync(() => component.Instance._OnMatchChange(false).AsTask()).GetAwaiter().GetResult();
        Assert.IsFalse(bound);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderTheBoundStateAsTheInitialOne()
    {
        var bound = true;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Bind(p => p.IsMatched, bound, v => bound = v);
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldLetTheBindingWinOverDefaultMatched()
    {
        // A bound IsMatched hands its own initial value over, so DefaultMatched has nothing to seed.
        var bound = false;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.Bind(p => p.IsMatched, bound, v => bound = v);
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.IsFalse(component.Instance.IsMatched);
        Assert.IsFalse(bound);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotMoveAOneWayBoundIsMatched()
    {
        // Without a Changed callback beside it the value belongs to the page that hands it over,
        // which is the same ownership rule every two-way bound parameter of the library follows.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.IsMatched, false);
            parameters.AddChildContent("<span>content</span>");
        });

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsMatched);
    }



    [TestMethod]
    public void BitMediaQueryShouldCallJsSetupWithCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual("(max-width: 600px)", invocation.Arguments[QueryArg]);
        Assert.IsNull(invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsSetupWithScreenQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.GtSm);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[QueryArg]);
        Assert.AreEqual("GtSm", invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    [DataRow(BitScreenQuery.Xs, "Xs")]
    [DataRow(BitScreenQuery.LtXxl, "LtXxl")]
    [DataRow(BitScreenQuery.GtXl, "GtXl")]
    [DataRow(BitScreenQuery.SmToMd, "SmToMd")]
    [DataRow(BitScreenQuery.LgToXl, "LgToXl")]
    public void BitMediaQueryShouldPassScreenQueryNameToJs(BitScreenQuery screenQuery, string expectedName)
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, screenQuery);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[QueryArg]);
        Assert.AreEqual(expectedName, invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldKeyTheJsListenerByItsOwnUniqueId()
    {
        // Not by the element id: two components may be given the same explicit Id, and a listener
        // keyed by it would then be torn down by whichever of them set up last.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Id, "shared-id");
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[KeyArg]);
        Assert.AreEqual("shared-id", invocation.Arguments[ElementIdArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldPreferCustomQueryOverScreenQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual("(max-width: 600px)", invocation.Arguments[QueryArg]);
        Assert.IsNull(invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldTreatBlankQueryAsAbsent()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, " ");
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Lg);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[QueryArg]);
        Assert.AreEqual("Lg", invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldTrimTheCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "  (max-width: 600px)  ");
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual("(max-width: 600px)", invocation.Arguments[QueryArg]);

        // The same query written with other whitespace around it is the same query, so the listener
        // is left alone rather than torn down and built again.
        component.Render(parameters => parameters.Add(p => p.Query, "(max-width: 600px)"));

        Assert.AreEqual(1, Setups(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotRepeatJsSetupForUnchangedCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render();

        Assert.AreEqual(1, Setups(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldRepeatJsSetupForChangedCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 900px)");
        });

        var setups = Setups(Context.JSInterop);
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("(max-width: 900px)", setups[1].Arguments[QueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldRepeatJsSetupForScreenQueryOnRerender()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render();

        // The effective query of a ScreenQuery is resolved on the JS side from the live theme
        // breakpoints, so setup is re-invoked on every render (JS reuses the listener when the
        // resolved expression is unchanged).
        Assert.AreEqual(2, Setups(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldSwitchBetweenScreenQueryAndCustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
        });

        var setups = Setups(Context.JSInterop);
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("Md", setups[0].Arguments[ScreenQueryArg]);
        Assert.AreEqual("(max-width: 600px)", setups[1].Arguments[QueryArg]);
        Assert.IsNull(setups[1].Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotReSetupWhenOnlyTheIdChangesForACustomQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Id, "first-id");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Id, "second-id");
        });

        // The listener is keyed by the component's own unique id, and a verbatim query is resolved
        // without reading anything off the element, so the new id changes nothing about it.
        Assert.AreEqual(1, Setups(Context.JSInterop).Count);
        Assert.AreEqual(0, Disposals(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendTheNewElementIdForAScreenQueryWhenTheIdChanges()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.Id, "first-id");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Id, "second-id");
        });

        // The element is the themed scope the breakpoints are read from, and a ScreenQuery re-invokes
        // setup on every render, so the scope follows the id without a teardown in between.
        var setups = Setups(Context.JSInterop);
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("first-id", setups[0].Arguments[ElementIdArg]);
        Assert.AreEqual("second-id", setups[1].Arguments[ElementIdArg]);
        Assert.AreEqual(component.Instance.UniqueId, setups[1].Arguments[KeyArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSetupAgainAfterQueryRemovedAndReadded()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, (string?)null);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 900px)");
        });

        var setups = Setups(Context.JSInterop);
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("(max-width: 900px)", setups[1].Arguments[QueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotCallJsSetupWithoutAnyQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.AreEqual(0, Setups(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsDisposeWhenQueryRemoved()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Query, (string?)null);
        });

        var disposal = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");
        Assert.AreEqual(component.Instance.UniqueId, disposal.Arguments[0]);
    }

    [TestMethod]
    public void BitMediaQueryShouldCallJsDisposeOnDispose()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.AddChildContent("<span>content</span>");
        });

        var uniqueId = component.Instance.UniqueId;

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        var disposal = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.dispose");
        Assert.AreEqual(uniqueId, disposal.Arguments[0]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotCallJsDisposeOnDisposeWithoutAnyQuery()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddChildContent("<span>content</span>");
        });

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        // No listener was ever set up, so there is nothing to tear down on the JS side.
        Assert.AreEqual(0, Disposals(Context.JSInterop).Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotInvokeOnChangeAfterDispose()
    {
        bool? changed = null;
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.OnChange, (bool v) => changed = v);
        });

        var instance = component.Instance;

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        // A notification racing the disposal must be ignored instead of rendering a disposed component.
        instance._OnMatchChange(true).GetAwaiter().GetResult();

        Assert.IsNull(changed);
    }



    [TestMethod]
    public void BitMediaQueryShouldSendNoBreakpointsWithoutAThemeProvider()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[BreakpointsArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendTheBreakpointsOfACascadingTheme()
    {
        var theme = new BitTheme { Layout = { Breakpoints = { Md = "700px", Lg = "900px" } } };

        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddCascadingValue((BitTheme?)theme);
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        var breakpoints = invocation.Arguments[BreakpointsArg] as Dictionary<string, string>;

        Assert.IsNotNull(breakpoints);
        // Only the overridden ones are sent; the rest is left to the CSS variables and the defaults,
        // so a provider re-valuing one breakpoint does not flatten the rest of the scale.
        Assert.AreEqual(2, breakpoints.Count);
        Assert.AreEqual("700px", breakpoints["md"]);
        Assert.AreEqual("900px", breakpoints["lg"]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendTheCascadingBreakpointsInNoWrapperModeToo()
    {
        // This is the case the DOM lookup cannot serve: there is no element of the component's own
        // for the --bit-bp-* variables of the enclosing provider to be read from.
        var theme = new BitTheme { Layout = { Breakpoints = { Sm = "500px" } } };

        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddCascadingValue((BitTheme?)theme);
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Xs);
            parameters.Add(p => p.NoWrapper, true);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        var breakpoints = invocation.Arguments[BreakpointsArg] as Dictionary<string, string>;

        Assert.IsNull(invocation.Arguments[ElementIdArg]);
        Assert.IsNotNull(breakpoints);
        Assert.AreEqual("500px", breakpoints["sm"]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendNoBreakpointsForACascadingThemeThatOverridesNone()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.AddCascadingValue((BitTheme?)new BitTheme());
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.AddChildContent("<span>content</span>");
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[BreakpointsArg]);
    }



    [TestMethod]
    public void BitMediaQueryShouldRenderAriaLabel()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.AriaLabel, "media query region");
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.AreEqual("media query region", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMediaQueryShouldRespectClassStyleIdAndDir()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red;");
            parameters.Add(p => p.Id, "custom-id");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.AreEqual("custom-id", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderWithoutRootElementWithNoWrapper()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Matched, Markup("<div class=\"matched\">Matched</div>"));
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);

        component.InvokeAsync(() => component.Instance._OnMatchChange(true).AsTask()).GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, component.FindAll(".matched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendTheElementIdOnlyWhenItRendersAnElement()
    {
        var withWrapper = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.Id, "wrapped");
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.AreEqual("wrapped", Setups(Context.JSInterop)[0].Arguments[ElementIdArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendNoElementIdInNoWrapperMode()
    {
        // The id is not the listener key, so nothing depends on it here: any other element carrying
        // it - the rendered content itself, when it is given the same id - is not the component's
        // themed scope, and the document root is read instead.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Id, "mdq-id");
            parameters.Add(p => p.DefaultMatched, true);
            parameters.AddChildContent("<div id=\"mdq-id\" class=\"child\">Child</div>");
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual("mdq-id", component.Find(".child").Id);

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[KeyArg]);
        Assert.IsNull(invocation.Arguments[ElementIdArg]);
        Assert.AreEqual("Md", invocation.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldSendNoElementIdWhenNothingIsRendered()
    {
        // An OnChange-only usage renders no element either, so there is nothing to read the theme
        // breakpoints from and the document root is what the JS side falls back to.
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.OnChange, (bool _) => { });
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.MediaQuery.setup");
        Assert.IsNull(invocation.Arguments[ElementIdArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldFollowTheNoWrapperToggleWithTheElementId()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.ScreenQuery, BitScreenQuery.Md);
            parameters.Add(p => p.Id, "mdq-id");
            parameters.AddChildContent("<span>content</span>");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
        });

        // A ScreenQuery re-invokes setup on every render, so the scope the breakpoints are read
        // from follows the toggle instead of staying at what the first setup was told.
        var setups = Setups(Context.JSInterop);
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual("mdq-id", setups[0].Arguments[ElementIdArg]);
        Assert.IsNull(setups[1].Arguments[ElementIdArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldRenderNothingWithNoWrapperWhenCollapsed()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.Add(p => p.NotMatched, Markup("<div class=\"notmatched\">NotMatched</div>"));
        });

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitMediaQueryShouldRespectVisibility()
    {
        var component = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.AddChildContent("<span>content</span>");
        });

        var root = component.Find(".bit-mdq");
        StringAssert.Contains(root.GetAttribute("style"), "display:none");
    }
}
