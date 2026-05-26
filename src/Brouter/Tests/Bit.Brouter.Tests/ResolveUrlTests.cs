using System.Globalization;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ResolveUrlTests : BunitTestContext
{
    /// <summary>
    /// Mounts a <see cref="Brouter"/> with a single named route at <paramref name="path"/>
    /// and returns an <see cref="IBrouter"/> ready for ResolveUrl/NavigateToName calls.
    /// </summary>
    private IBrouter MountWithNamedRoute(string name, string path)
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/__test__");

        RenderComponent<NamedRouteHost>(p => p
            .Add(h => h.Name, name)
            .Add(h => h.Path, path));

        return Services.GetRequiredService<IBrouter>();
    }

    [TestMethod]
    public void Resolves_required_parameter()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        var url = brouter.ResolveUrl("user", new Dictionary<string, object?> { ["id"] = 42 });

        Assert.AreEqual("/users/42", url);
    }

    [TestMethod]
    public void Resolves_parameter_with_case_insensitive_key()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        // Caller used "ID" but the template parameter is "id" - should still bind.
        var url = brouter.ResolveUrl("user", new Dictionary<string, object?> { ["ID"] = 7 });

        Assert.AreEqual("/users/7", url);
    }

    [TestMethod]
    public void Throws_when_route_name_not_registered()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            brouter.ResolveUrl("missing", new Dictionary<string, object?> { ["id"] = 1 }));
    }

    [TestMethod]
    public void Throws_when_required_parameter_is_missing()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        Assert.ThrowsExactly<ArgumentException>(() => brouter.ResolveUrl("user"));
    }

    [TestMethod]
    public void Optional_parameter_present_is_emitted()
    {
        var brouter = MountWithNamedRoute("profile", "/profile/{username?}");

        var url = brouter.ResolveUrl("profile",
            new Dictionary<string, object?> { ["username"] = "saleh" });

        Assert.AreEqual("/profile/saleh", url);
    }

    [TestMethod]
    public void Optional_parameter_absent_trims_trailing_slash()
    {
        var brouter = MountWithNamedRoute("profile", "/profile/{username?}");

        // No params at all: trailing '/' for the absent optional should be dropped.
        Assert.AreEqual("/profile", brouter.ResolveUrl("profile"));

        // Explicit null value: same behavior.
        Assert.AreEqual("/profile",
            brouter.ResolveUrl("profile", new Dictionary<string, object?> { ["username"] = null }));
    }

    [TestMethod]
    public void Multiple_trailing_optionals_emit_all_when_supplied()
    {
        var brouter = MountWithNamedRoute("range", "/range/{from?}/{to?}");

        var url = brouter.ResolveUrl("range",
            new Dictionary<string, object?> { ["from"] = "1", ["to"] = "10" });

        Assert.AreEqual("/range/1/10", url);
    }

    [TestMethod]
    public void Multiple_trailing_optionals_all_absent_trims_tail()
    {
        var brouter = MountWithNamedRoute("range", "/range/{from?}/{to?}");

        Assert.AreEqual("/range", brouter.ResolveUrl("range"));
    }

    [TestMethod]
    public void Trailing_optional_absent_with_earlier_optional_supplied_trims_only_the_trailing_slash()
    {
        var brouter = MountWithNamedRoute("range", "/range/{from?}/{to?}");

        // Supplying only the leading optional is well-defined: emit it, drop the trailing slot.
        var url = brouter.ResolveUrl("range",
            new Dictionary<string, object?> { ["from"] = "1" });

        Assert.AreEqual("/range/1", url);
    }

    [TestMethod]
    public void Throws_when_earlier_optional_omitted_but_later_optional_supplied()
    {
        var brouter = MountWithNamedRoute("range", "/range/{from?}/{to?}");

        // Without this guard the URL would be "/range/10" and the matcher would bind it as
        // from="10", silently shifting the value into the wrong parameter slot.
        Assert.ThrowsExactly<ArgumentException>(() => brouter.ResolveUrl("range",
            new Dictionary<string, object?> { ["to"] = "10" }));
    }

    [TestMethod]
    public void Catch_all_parameter_preserves_internal_slashes_and_encodes_each_segment()
    {
        var brouter = MountWithNamedRoute("posts", "/posts/{**slug}");

        var url = brouter.ResolveUrl("posts",
            new Dictionary<string, object?> { ["slug"] = "2026/05/hello world" });

        // Slashes are preserved as path separators; each segment is percent-encoded individually
        // (so the space in "hello world" becomes %20 but the slashes don't).
        Assert.AreEqual("/posts/2026/05/hello%20world", url);
    }

    [TestMethod]
    public void Catch_all_parameter_with_empty_value_drops_trailing_slash()
    {
        var brouter = MountWithNamedRoute("posts", "/posts/{**slug}");

        var url = brouter.ResolveUrl("posts",
            new Dictionary<string, object?> { ["slug"] = string.Empty });

        Assert.AreEqual("/posts", url);
    }

    [TestMethod]
    public void Regular_parameter_value_is_percent_encoded()
    {
        var brouter = MountWithNamedRoute("user", "/users/{name}");

        var url = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["name"] = "john doe/admin" });

        // Non-catch-all parameter encodes the entire value, including '/'.
        Assert.AreEqual("/users/john%20doe%2Fadmin", url);
    }

    [TestMethod]
    public void Throws_when_template_contains_literal_wildcard()
    {
        // A literal '*' segment can't be resolved back into a URL - there's no value to substitute.
        var brouter = MountWithNamedRoute("wild", "/files/*");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            brouter.ResolveUrl("wild"));
    }

    [TestMethod]
    public void Query_string_is_appended_with_leading_question_mark_added_when_missing()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        var withPrefix = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["id"] = 1 }, query: "?tab=info");
        var withoutPrefix = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["id"] = 1 }, query: "tab=info");

        Assert.AreEqual("/users/1?tab=info", withPrefix);
        Assert.AreEqual("/users/1?tab=info", withoutPrefix);
    }

    [TestMethod]
    public void Boolean_is_formatted_as_lowercase()
    {
        var brouter = MountWithNamedRoute("flag", "/flag/{enabled}");

        Assert.AreEqual("/flag/true",
            brouter.ResolveUrl("flag", new Dictionary<string, object?> { ["enabled"] = true }));
        Assert.AreEqual("/flag/false",
            brouter.ResolveUrl("flag", new Dictionary<string, object?> { ["enabled"] = false }));
    }

    [TestMethod]
    public void Enum_is_formatted_as_symbolic_name()
    {
        var brouter = MountWithNamedRoute("day", "/day/{value}");

        var url = brouter.ResolveUrl("day",
            new Dictionary<string, object?> { ["value"] = DayOfWeek.Tuesday });

        Assert.AreEqual("/day/Tuesday", url);
    }

    [TestMethod]
    public void DateTime_is_formatted_as_round_trip_invariant()
    {
        var brouter = MountWithNamedRoute("when", "/when/{ts}");
        var dt = new DateTime(2026, 5, 23, 13, 45, 7, DateTimeKind.Utc);

        var url = brouter.ResolveUrl("when",
            new Dictionary<string, object?> { ["ts"] = dt });

        // "o" specifier is lossless and culture-independent; the ':' characters are percent-encoded.
        Assert.AreEqual("/when/" + Uri.EscapeDataString(dt.ToString("o", CultureInfo.InvariantCulture)), url);
    }

    [TestMethod]
    public void Numeric_values_use_invariant_culture()
    {
        // Switch to a culture that uses ',' as decimal separator. Without invariant formatting,
        // 1.5 would surface as "1,5" and break the URL.
        // Mutate Thread.CurrentThread culture (and UI culture) so the change is scoped to this
        // test thread instead of leaking via the process-wide CultureInfo.CurrentCulture default.
        var previousThreadCulture = Thread.CurrentThread.CurrentCulture;
        var previousThreadUICulture = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
        try
        {
            var brouter = MountWithNamedRoute("price", "/price/{amount}");

            var url = brouter.ResolveUrl("price",
                new Dictionary<string, object?> { ["amount"] = 1.5 });

            Assert.AreEqual("/price/1.5", url);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousThreadCulture;
            Thread.CurrentThread.CurrentUICulture = previousThreadUICulture;
        }
    }
}
