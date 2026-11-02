using System.Runtime.CompilerServices;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// "Order 1042 not found", "Order 1043 not found", "Order 1044 not found": one problem, thrown from one place, that
/// reads as three because each message carries its own id. Grouping must merge those and nothing else.
/// </summary>
[TestClass]
public class ErrorGroupsTests : BunitContext
{
    public ErrorGroupsTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    [DataRow("Order 1042 not found", "Order {n} not found")]
    [DataRow("Order 42 not found", "Order 42 not found")]
    [DataRow("Response status code does not indicate success: 404 (Not Found).", "Response status code does not indicate success: 404 (Not Found).")]
    [DataRow("Cart 3f2504e0-4f89-11d3-9a0c-0305e82c3301 is closed", "Cart {guid} is closed")]
    [DataRow("Invalid token 9f86d081884c7d659a2feaa0c55ad015", "Invalid token {hex}")]
    [DataRow("The given key 'sku-77' was not present in the dictionary.", "The given key '…' was not present in the dictionary.")]
    [DataRow("Cannot read properties of undefined (reading \"price\")", "Cannot read properties of undefined (reading \"…\")")]
    [DataRow("The user can't open it, it's locked", "The user can't open it, it's locked")]
    [DataRow("GET https://shop.test/api/orders/1042?page=2 failed", "GET https://shop.test/api/orders/{id}?page failed")]
    [DataRow("Timed out after 100 seconds", "Timed out after 100 seconds")]
    [DataRow("Version 10.0.1 is required", "Version 10.0.1 is required")]
    [DataRow("GET /api/orders/42 returned 404 Not Found", "GET /api/orders/{id} returned 404 Not Found")]
    [DataRow("GET /api/products?q=abc returned 500 Internal Server Error", "GET /api/products?q returned 500 Internal Server Error")]
    [DataRow("No answer from http://localhost:5001/api/orders/1042.", "No answer from http://localhost:5001/api/orders/{id}.")]
    [DataRow("Connection refused (localhost:5001)", "Connection refused (localhost:5001)")]
    [DataRow("Session expired at 2026-10-23T10:30:00Z", "Session expired at {time}")]
    public void A_message_template_replaces_values_that_identify_an_instance_and_keeps_those_that_name_a_failure(string message, string template)
    {
        Assert.AreEqual(template, BevErrorGroups.Template(message));
    }

    [TestMethod]
    public void Errors_from_one_place_with_different_ids_are_one_group_and_other_failures_are_not()
    {
        var a = Session.Errors.Record(Thrown(1042), "event");
        var b = Session.Errors.Record(Thrown(1043), "event");
        var c = Session.Errors.Record(new InvalidOperationException("Order 1044 not found"), "event");
        var other = Session.Errors.Record(new TimeoutException("Order 1045 not found"), "event");

        Assert.AreNotEqual(a.Id, b.Id, "two records: their messages differ");
        Assert.AreEqual(a.Group, b.Group);
        Assert.AreEqual("Order {n} not found", a.MessageTemplate);
        Assert.AreNotEqual(a.Group, c.Group, "not thrown from the same place");
        Assert.AreNotEqual(c.Group, other.Group, "another exception type");
    }

    [TestMethod]
    public void Throw_sites_stay_apart_whatever_their_line_numbers_and_however_the_stack_starts()
    {
        BevErrorRecord Error(string message, string stack, string type = "System.ArgumentNullException", string source = "render") =>
            Session.Errors.Record(null, source, message, exceptionType: type, stackTrace: stack);

        // Line numbers of four digits are where, not which.
        var first = Error("Value cannot be null. (Parameter 'order')", "   at App.BigService.Ship() in C:\\src\\BigService.cs:line 1042");
        var other = Error("Value cannot be null. (Parameter 'customer')", "   at App.BigService.Bill() in C:\\src\\BigService.cs:line 1311");
        var again = Error("Value cannot be null. (Parameter 'invoice')", "   at App.BigService.Ship() in C:\\src\\BigService.cs:line 1042");
        Assert.AreNotEqual(first.Group, other.Group);
        Assert.AreEqual(first.Group, again.Group);

        // A JavaScript stack starts with its message; the first frame is where.
        var cart = Error("Cannot read properties of undefined (reading 'price')", "TypeError: Cannot read properties of undefined (reading 'price')\n    at addToCart (https://shop.test/js/cart.js:12:5)", "TypeError", "javascript");
        var profile = Error("Cannot read properties of undefined (reading 'name')", "TypeError: Cannot read properties of undefined (reading 'name')\n    at showProfile (https://shop.test/js/profile.js:88:9)", "TypeError", "javascript");
        var cartAgain = Error("Cannot read properties of undefined (reading 'total')", "TypeError: Cannot read properties of undefined (reading 'total')\n    at addToCart (https://shop.test/js/cart.js:12:5)", "TypeError", "javascript");
        Assert.AreNotEqual(cart.Group, profile.Group);
        Assert.AreEqual(cart.Group, cartAgain.Group);

        // Without a PDB, a shared throw helper is not where: the first application frame is.
        var orders = Error("Value cannot be null. (Parameter 'order')", "   at System.ArgumentNullException.Throw(String paramName)\n   at App.OrderService.Find(Order order)");
        var customers = Error("Value cannot be null. (Parameter 'customer')", "   at System.ArgumentNullException.Throw(String paramName)\n   at App.CustomerService.Find(Customer customer)");
        Assert.AreNotEqual(orders.Group, customers.Group);

        // Only framework frames: the quoted names are what tells two missing registrations apart.
        const string activation = "   at Microsoft.AspNetCore.Components.ComponentFactory.PerformPropertyInjection(IServiceProvider serviceProvider, IComponent instance)";
        var api = Error("Cannot provide a value for property 'Api' on type 'App.Pages.Orders'. There is no registered service of type 'App.IOrderApi'.", activation, "System.InvalidOperationException");
        var cartService = Error("Cannot provide a value for property 'Cart' on type 'App.Pages.Checkout'. There is no registered service of type 'App.ICartService'.", activation, "System.InvalidOperationException");
        Assert.AreNotEqual(api.Group, cartService.Group);
        Assert.Contains("'App.IOrderApi'", api.MessageTemplate, StringComparison.Ordinal);
    }

    [TestMethod]
    public void An_error_still_counts_after_its_record_left_the_buffer()
    {
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.MaxErrors = 3;
            o.Diagnostics.RepeatedErrorCount = 5;
        });
        using (scope)
        {
            foreach (var id in new[] { 1042, 1043, 1044, 1045, 1046 })
            {
                session.Errors.Record(Thrown(id), "event");
            }

            Assert.AreEqual(3, session.Errors.Count, "precondition: two records were evicted");
            session.Diagnostics.Evaluate(force: true);

            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "error.repeated", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("InvalidOperationException: Order {n} not found occurred 5 times with 5 different messages", finding.Title);
        }
    }

    [TestMethod]
    public void Returning_to_the_groups_shows_every_group_again()
    {
        var panel = RenderPanel();
        foreach (var id in new[] { 1042, 1043, 1044 })
        {
            Session.Errors.Record(Thrown(id), "event");
        }

        Session.Errors.Record(new TimeoutException("The request timed out"), "http");
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Errors", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Groups").Click();
        panel.FindAll("tbody tr.bev-row")[0].QuerySelector("button")!.Click();
        Assert.HasCount(3, panel.FindAll("tbody tr.bev-row"));

        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Groups").Click();
        Assert.HasCount(2, panel.FindAll("tbody tr.bev-row"));
        Assert.Contains("2 groups · 4 errors", panel.Markup, StringComparison.Ordinal);

        panel.Find("input[aria-label='Filter errors']").Input("source:http");
        Assert.HasCount(1, panel.FindAll("tbody tr.bev-row"), "the http group, not \"No errors\"");
    }

    [TestMethod]
    public void Repeated_errors_with_different_ids_add_up_to_one_finding()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.RepeatedErrorCount = 3);
        using (scope)
        {
            foreach (var id in new[] { 1042, 1043, 1044 })
            {
                session.Errors.Record(Thrown(id), "event");
            }

            session.Diagnostics.Evaluate(force: true);

            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "error.repeated", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("InvalidOperationException: Order {n} not found occurred 3 times with 3 different messages", finding.Title);
        }
    }

    [TestMethod]
    public void The_errors_panel_shows_groups_and_opens_one_as_its_errors()
    {
        var panel = RenderPanel();
        foreach (var id in new[] { 1042, 1043, 1044 })
        {
            Session.Errors.Record(Thrown(id), "event");
        }

        Session.Errors.Record(new TimeoutException("The request timed out"), "http");
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Errors", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.HasCount(4, panel.FindAll("tbody tr.bev-row"));

        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Groups").Click();
        var rows = panel.FindAll("tbody tr.bev-row");
        Assert.HasCount(2, rows);
        var orders = rows[0].QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToArray();
        Assert.StartsWith("InvalidOperationException: Order {n} not found", orders[2], "the most occurrences first");
        Assert.Contains("@ ErrorGroupsTests.cs:", orders[2], StringComparison.Ordinal, "with where it was thrown");
        Assert.AreEqual("3", orders[4], "three different messages");
        Assert.AreEqual("3", orders[5], "three occurrences");

        rows[0].QuerySelector("button")!.Click();
        Assert.AreEqual("true", panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Errors").GetAttribute("aria-pressed"));
        Assert.HasCount(3, panel.FindAll("tbody tr.bev-row"));
        Assert.Contains("Order {n} not found", panel.Find(".bev-chip.bev-on").TextContent, StringComparison.Ordinal);
        Assert.Contains("Order 1044 not found", panel.Find(".bev-right").TextContent, StringComparison.Ordinal, "the latest error of the group is selected");

        panel.Find("button[aria-label='Show every error']").Click();
        Assert.HasCount(4, panel.FindAll("tbody tr.bev-row"));
    }

    [TestMethod]
    public void Exports_and_the_report_carry_the_groups()
    {
        foreach (var id in new[] { 1042, 1043 })
        {
            Session.Errors.Record(Thrown(id), "event");
        }

        using var json = JsonDocument.Parse(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Sections = new HashSet<string> { "errors" } }));
        Assert.IsTrue(json.RootElement.GetProperty("errors").EnumerateArray().All(e => e.GetProperty("messageTemplate").GetString() == "Order {n} not found"));
        var group = Assert.ContainsSingle(json.RootElement.GetProperty("errorGroups").EnumerateArray().ToList());
        Assert.AreEqual(2, group.GetProperty("occurrences").GetInt32());
        Assert.AreEqual(2, group.GetProperty("distinctMessages").GetInt32());

        var report = BevSessionExporter.ToMarkdownReport(Session);
        Assert.Contains("One problem with different values in its messages:", report, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException: Order {n} not found (×2, 2 messages)", report, StringComparison.Ordinal);
    }

    // One throw site, so every exception has the same source location.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception Thrown(int orderId)
    {
        try
        {
            throw new InvalidOperationException("Order " + orderId + " not found");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        module.SetupVoid("savePrefs", _ => true).SetVoidResult();
        return Render<BevToolsPanel>();
    }
}
