using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

public sealed record LoadedUser(string Name, int Age);

[TestClass]
public class RouteDataCascadeTests : BunitTestContext
{
    [TestMethod]
    public void RouteData_and_RouteMeta_cascade_as_typed_wrappers_matched_by_type()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/typed");

        var cut = RenderComponent<TypedDataHost>();

        // TypedValueReader declares [CascadingParameter] without a Name, so this also proves
        // the wrapper types are matched by type alone.
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("saleh", cut.Find("[data-testid=name]").TextContent);
            Assert.AreEqual("42", cut.Find("[data-testid=age]").TextContent);
            Assert.AreEqual("admin-area", cut.Find("[data-testid=meta]").TextContent);
        });
    }

    [TestMethod]
    public void Get_returns_value_when_type_matches()
    {
        var data = new BrouterRouteData(new LoadedUser("a", 1));

        Assert.AreEqual(new LoadedUser("a", 1), data.Get<LoadedUser>());
        Assert.IsTrue(data.HasValue);
    }

    [TestMethod]
    public void Get_throws_with_distinct_messages_for_absent_and_mismatched_values()
    {
        var empty = BrouterRouteData.Empty;
        var mismatched = new BrouterRouteData("a string");

        var absentEx = Assert.ThrowsExactly<InvalidOperationException>(() => empty.Get<LoadedUser>());
        var mismatchEx = Assert.ThrowsExactly<InvalidOperationException>(() => mismatched.Get<LoadedUser>());

        StringAssert.Contains(absentEx.Message, "No route data value is present");
        StringAssert.Contains(mismatchEx.Message, nameof(String));
        StringAssert.Contains(mismatchEx.Message, nameof(LoadedUser));
    }

    [TestMethod]
    public void TryGet_and_GetOrDefault_handle_absence_and_mismatch()
    {
        var data = new BrouterRouteData(new LoadedUser("a", 1));

        Assert.IsTrue(data.TryGet<LoadedUser>(out var user));
        Assert.AreEqual("a", user.Name);

        Assert.IsFalse(data.TryGet<string>(out _));
        Assert.IsFalse(BrouterRouteData.Empty.TryGet<LoadedUser>(out _));

        Assert.IsNull(data.GetOrDefault<string>());
        Assert.AreEqual("fallback", BrouterRouteData.Empty.GetOrDefault("fallback"));
    }

    [TestMethod]
    public void Meta_wrapper_reports_absence_via_HasValue()
    {
        Assert.IsFalse(BrouterRouteMeta.Empty.HasValue);
        Assert.IsNull(BrouterRouteMeta.Empty.Value);
        Assert.IsTrue(new BrouterRouteMeta("m").HasValue);
    }
}
