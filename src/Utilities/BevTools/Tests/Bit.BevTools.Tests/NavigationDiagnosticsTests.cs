using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>What can stop a navigation, and navigations that feed each other.</summary>
[TestClass]
public class NavigationDiagnosticsTests : BunitContext
{
    public NavigationDiagnosticsTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void Navigation_locks_and_location_changing_handlers_are_listed_with_their_component()
    {
        Render<UnsavedChangesEditor>();
        Session.Components.Refresh(force: true);

        var guards = BevNavigationGuards.Read(Session);
        var navigationLock = Assert.ContainsSingle(g => g.Kind == "NavigationLock", guards);
        Assert.AreEqual("UnsavedChangesEditor", navigationLock.OwnerName);
        StringAssert.Contains(navigationLock.Description, "confirm leaving the site");
        StringAssert.Contains(navigationLock.Description, "UnsavedChangesEditor.OnBeforeNavigation");

        var handler = Assert.ContainsSingle(g => g.Kind == "LocationChanging handler", guards);
        Assert.AreEqual("UnsavedChangesEditor", handler.OwnerName);
        StringAssert.Contains(handler.Description, "UnsavedChangesEditor.Guard");
    }

    private void Navigation(string title, double secondsAgo, string detail = "programmatic", long? fromEvent = null) => Session.Timeline.Record(new BevToolsEvent
    {
        Kind = BevToolsEventKind.Navigation,
        Category = "navigation",
        Title = title,
        Detail = detail,
        ParentEventId = fromEvent,
        Timestamp = DateTimeOffset.UtcNow.AddSeconds(-secondsAgo),
    });

    [TestMethod]
    public void Two_pages_redirecting_to_each_other_are_a_loop()
    {
        foreach (var i in Enumerable.Range(0, 6))
        {
            Navigation(i % 2 == 0 ? "Navigate to /login" : "Navigate to /account", 0);
        }

        Session.Diagnostics.Evaluate(force: true);
        var loop = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual(BevDiagnosticSeverity.Error, loop.Severity);
        StringAssert.Contains(loop.Title, "Navigate to /login");
        StringAssert.Contains(loop.Title, "Navigate to /account");
        StringAssert.StartsWith(loop.Evidence, "6 navigations");
        StringAssert.Contains(loop.Evidence, "every one made by code");
    }

    [TestMethod]
    public void A_redirect_made_while_the_previous_navigation_ran_is_still_code()
    {
        // .NET 10: each page redirects in OnInitialized, inside the router's activity for the navigation before it.
        long? previous = null;
        foreach (var i in Enumerable.Range(0, 6))
        {
            previous = Session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.Navigation,
                Category = "navigation",
                Title = i % 2 == 0 ? "Navigate to /login" : "Navigate to /account",
                ParentEventId = previous,
            });
        }

        Session.Diagnostics.Evaluate(force: true);
        Assert.ContainsSingle(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void A_loop_that_ran_while_nobody_looked_is_still_found_later()
    {
        // The panel was closed: diagnostics run a minute after the loop, not within its 10 seconds.
        foreach (var i in Enumerable.Range(0, 6))
        {
            Navigation(i % 2 == 0 ? "Navigate to /login" : "Navigate to /account", 60);
        }

        Session.Diagnostics.Evaluate(force: true);
        Assert.ContainsSingle(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Navigation_a_person_makes_is_neither_a_loop_nor_a_storm()
    {
        // Back and forth between a list and its details by links, and a search box writing the query string as one types.
        foreach (var i in Enumerable.Range(0, 8))
        {
            Navigation(i % 2 == 0 ? "Navigate to /products" : "Navigate to /products/{id}", 0, detail: "intercepted link");
        }

        foreach (var i in Enumerable.Range(0, 12))
        {
            var keystroke = Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Category = "ui-event", Title = "oninput → SearchBox.OnInput" });
            Navigation("Navigate to /search?q=" + i, 0, fromEvent: keystroke);
        }

        Session.Diagnostics.Evaluate(force: true);
        Assert.DoesNotContain(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Ordinary_navigation_is_neither_a_loop_nor_a_storm()
    {
        foreach (var page in new[] { "/", "/products", "/products/1", "/products", "/cart", "/checkout" })
        {
            Navigation("Navigate to " + page, 0);
        }

        Session.Diagnostics.Evaluate(force: true);
        Assert.DoesNotContain(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void A_burst_of_navigations_is_a_storm()
    {
        foreach (var i in Enumerable.Range(0, 10))
        {
            Navigation("Navigate to /step/" + i, 0);
        }

        Session.Diagnostics.Evaluate(force: true);
        var storm = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "navigation.loop", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("10+ navigations within 2 s", storm.Title);
    }

    [TestMethod]
    public void The_routing_panel_shows_guards_and_what_each_navigation_rendered()
    {
        var panel = Render<BevToolsPanel>();
        Render<UnsavedChangesEditor>();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere");
        Render<ParentComponent>();
        panel.InvokeAsync(() => panel.Instance.ShowPanel("routing"));

        panel.WaitForAssertion(() =>
        {
            var text = panel.Find(".bev-body").TextContent;
            StringAssert.Contains(text, "NavigationLock");
            StringAssert.Contains(text, "UnsavedChangesEditor.Guard");
            var row = panel.FindAll(".bev-body tr").First(r => r.TextContent.Contains("Navigate to /somewhere", StringComparison.Ordinal));
            Assert.AreEqual("2", row.Children[3].TextContent.Trim(), "the parent and its child rendered after it");
        });
    }
}

public class UnsavedChangesEditor : ComponentBase, IDisposable
{
    private IDisposable? _registration;

    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized() => _registration = Navigation.RegisterLocationChangingHandler(Guard);

    private ValueTask Guard(LocationChangingContext context) => ValueTask.CompletedTask;

    private void OnBeforeNavigation(LocationChangingContext context)
    {
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<NavigationLock>(0);
        builder.AddComponentParameter(1, nameof(NavigationLock.ConfirmExternalNavigation), true);
        builder.AddComponentParameter(2, nameof(NavigationLock.OnBeforeInternalNavigation), EventCallback.Factory.Create<LocationChangingContext>(this, OnBeforeNavigation));
        builder.CloseComponent();
    }

    public void Dispose() => _registration?.Dispose();
}
