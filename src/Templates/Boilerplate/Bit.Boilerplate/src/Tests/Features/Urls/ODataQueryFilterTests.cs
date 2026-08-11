using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Urls;

/// <summary>
/// OData binds <c>and</c> tighter than <c>or</c>, so an accumulate-a-predicate API that concatenates without
/// parentheses silently re-associates what the caller already built: <c>A or B and C</c> is <c>A or (B and C)</c>.
/// The sequence <c>.docs/03</c> teaches - set <c>Filter</c>, then narrow it with <c>AndFilter</c> - hits exactly that
/// the moment the seed filter contains a top-level <c>or</c>, and the server answers with a superset of the intended
/// rows without any error.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ODataQueryFilterTests
{
    [TestMethod]
    public void AndFilter_Should_ParenthesizeAFilterThatAlreadyContainsAnOr()
    {
        var query = new ODataQuery { Filter = "contains(Name,'a') or contains(Code,'a')" };

        query.AndFilter = "IsActive eq true";

        Assert.AreEqual("(contains(Name,'a') or contains(Code,'a')) and (IsActive eq true)", query.Filter);
    }

    [TestMethod]
    public void OrFilter_Should_ParenthesizeBothOperands()
    {
        var query = new ODataQuery { Filter = "IsActive eq true" };

        query.OrFilter = "IsPinned eq true";

        Assert.AreEqual("(IsActive eq true) or (IsPinned eq true)", query.Filter);
    }

    /// <summary>
    /// Callers build their clauses conditionally, so an empty operand is normal. Appending it would produce
    /// <c>(X) and ()</c>, which the server rejects outright - a 400 on a grid that merely had one filter box empty.
    /// </summary>
    [TestMethod]
    public void AnEmptyOperand_Should_LeaveTheFilterUntouched()
    {
        var query = new ODataQuery { Filter = "IsActive eq true" };

        query.AndFilter = "";
        query.OrFilter = "";
        query.AndFilter = null!;

        Assert.AreEqual("IsActive eq true", query.Filter);
    }

    [TestMethod]
    public void AnEmptyOperand_Should_NotSeedAnEmptyFilter()
    {
        var query = new ODataQuery { AndFilter = "" };

        Assert.IsNull(query.Filter);
    }

    [TestMethod]
    public void TheFirstFilter_Should_BeSetAsIs()
    {
        Assert.AreEqual("IsActive eq true", new ODataQuery { AndFilter = "IsActive eq true" }.Filter);

        // Empty, not just null: ToString() guards with IsNullOrEmpty, so an empty Filter is a state this type expects -
        // and appending to it used to produce a leading " and ", which is not valid OData.
        Assert.AreEqual("IsActive eq true", new ODataQuery { Filter = "", AndFilter = "IsActive eq true" }.Filter);
    }
}
