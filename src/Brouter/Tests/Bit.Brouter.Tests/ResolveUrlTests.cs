using System.Globalization;
using Bit.Brouter;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Bit.Brouter.Tests;

public class ResolveUrlTests : TestContext
{
    public ResolveUrlTests()
    {
        Services.AddBitBrouterServices();
    }

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

    [Fact]
    public void Resolves_required_parameter()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        var url = brouter.ResolveUrl("user", new Dictionary<string, object?> { ["id"] = 42 });

        Assert.Equal("/users/42", url);
    }

    [Fact]
    public void Resolves_parameter_with_case_insensitive_key()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        // Caller used "ID" but the template parameter is "id" — should still bind.
        var url = brouter.ResolveUrl("user", new Dictionary<string, object?> { ["ID"] = 7 });

        Assert.Equal("/users/7", url);
    }

    [Fact]
    public void Throws_when_route_name_not_registered()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        Assert.Throws<InvalidOperationException>(() =>
            brouter.ResolveUrl("missing", new Dictionary<string, object?> { ["id"] = 1 }));
    }

    [Fact]
    public void Throws_when_required_parameter_is_missing()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        Assert.Throws<ArgumentException>(() => brouter.ResolveUrl("user"));
    }

    [Fact]
    public void Optional_parameter_present_is_emitted()
    {
        var brouter = MountWithNamedRoute("profile", "/profile/{username?}");

        var url = brouter.ResolveUrl("profile",
            new Dictionary<string, object?> { ["username"] = "saleh" });

        Assert.Equal("/profile/saleh", url);
    }

    [Fact]
    public void Optional_parameter_absent_trims_trailing_slash()
    {
        var brouter = MountWithNamedRoute("profile", "/profile/{username?}");

        // No params at all: trailing '/' for the absent optional should be dropped.
        Assert.Equal("/profile", brouter.ResolveUrl("profile"));

        // Explicit null value: same behavior.
        Assert.Equal("/profile",
            brouter.ResolveUrl("profile", new Dictionary<string, object?> { ["username"] = null }));
    }

    [Fact]
    public void Catch_all_parameter_preserves_internal_slashes_and_encodes_each_segment()
    {
        var brouter = MountWithNamedRoute("posts", "/posts/{**slug}");

        var url = brouter.ResolveUrl("posts",
            new Dictionary<string, object?> { ["slug"] = "2026/05/hello world" });

        // Slashes are preserved as path separators; each segment is percent-encoded individually
        // (so the space in "hello world" becomes %20 but the slashes don't).
        Assert.Equal("/posts/2026/05/hello%20world", url);
    }

    [Fact]
    public void Catch_all_parameter_with_empty_value_drops_trailing_slash()
    {
        var brouter = MountWithNamedRoute("posts", "/posts/{**slug}");

        var url = brouter.ResolveUrl("posts",
            new Dictionary<string, object?> { ["slug"] = string.Empty });

        Assert.Equal("/posts", url);
    }

    [Fact]
    public void Regular_parameter_value_is_percent_encoded()
    {
        var brouter = MountWithNamedRoute("user", "/users/{name}");

        var url = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["name"] = "john doe/admin" });

        // Non-catch-all parameter encodes the entire value, including '/'.
        Assert.Equal("/users/john%20doe%2Fadmin", url);
    }

    [Fact]
    public void Throws_when_template_contains_literal_wildcard()
    {
        // A literal '*' segment can't be resolved back into a URL — there's no value to substitute.
        var brouter = MountWithNamedRoute("wild", "/files/*");

        Assert.Throws<InvalidOperationException>(() =>
            brouter.ResolveUrl("wild"));
    }

    [Fact]
    public void Query_string_is_appended_with_leading_question_mark_added_when_missing()
    {
        var brouter = MountWithNamedRoute("user", "/users/{id}");

        var withPrefix = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["id"] = 1 }, query: "?tab=info");
        var withoutPrefix = brouter.ResolveUrl("user",
            new Dictionary<string, object?> { ["id"] = 1 }, query: "tab=info");

        Assert.Equal("/users/1?tab=info", withPrefix);
        Assert.Equal("/users/1?tab=info", withoutPrefix);
    }

    [Fact]
    public void Boolean_is_formatted_as_lowercase()
    {
        var brouter = MountWithNamedRoute("flag", "/flag/{enabled}");

        Assert.Equal("/flag/true",
            brouter.ResolveUrl("flag", new Dictionary<string, object?> { ["enabled"] = true }));
        Assert.Equal("/flag/false",
            brouter.ResolveUrl("flag", new Dictionary<string, object?> { ["enabled"] = false }));
    }

    [Fact]
    public void Enum_is_formatted_as_symbolic_name()
    {
        var brouter = MountWithNamedRoute("day", "/day/{value}");

        var url = brouter.ResolveUrl("day",
            new Dictionary<string, object?> { ["value"] = DayOfWeek.Tuesday });

        Assert.Equal("/day/Tuesday", url);
    }

    [Fact]
    public void DateTime_is_formatted_as_round_trip_invariant()
    {
        var brouter = MountWithNamedRoute("when", "/when/{ts}");
        var dt = new DateTime(2026, 5, 23, 13, 45, 7, DateTimeKind.Utc);

        var url = brouter.ResolveUrl("when",
            new Dictionary<string, object?> { ["ts"] = dt });

        // "o" specifier is lossless and culture-independent; the ':' characters are percent-encoded.
        Assert.Equal("/when/" + Uri.EscapeDataString(dt.ToString("o", CultureInfo.InvariantCulture)), url);
    }

    [Fact]
    public void Numeric_values_use_invariant_culture()
    {
        // Switch to a culture that uses ',' as decimal separator. Without invariant formatting,
        // 1.5 would surface as "1,5" and break the URL.
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var brouter = MountWithNamedRoute("price", "/price/{amount}");

            var url = brouter.ResolveUrl("price",
                new Dictionary<string, object?> { ["amount"] = 1.5 });

            Assert.Equal("/price/1.5", url);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
