using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Tests.Features.Tenants;

/// <summary>
/// <see cref="ReservedTenantNames"/> is the only thing standing between self-service tenant creation and a signed-up
/// stranger capturing the deployment's own host.
/// <para>
/// <c>Tenant.Name</c> is the sub domain resolution key (See <c>TenantProvider</c>) and <c>Tenant.Domain</c> is matched
/// against the request host, so a tenant named after the primary host's first label - or holding the primary host as
/// its custom domain - answers <b>every anonymous request</b> to that host, and additionally decides which tenant's
/// <c>demo</c> role each new sign-up is granted (See <c>UserManagerExtensions.CreateUserWithDemoRole</c>). Uniqueness
/// alone does not help, because no tenant owns the deployment's own label.
/// </para>
/// <para>
/// Everything here is a pure static function over strings, which is exactly why it earns a unit test rather than an
/// integration one - and why it is easy to get subtly wrong. Three of the rules below are non-obvious and each of them
/// would silently either open the hole or lock out legitimate tenants: the <c>Length &gt; 2</c> gate that mirrors
/// <c>TenantProvider</c>, the parent zone a sub domain host reserves, and the suffix boundary that stops
/// <c>myapp.com.attacker.com</c> matching <c>myapp.com</c>.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ReservedTenantNameTests
{
    /// <summary>
    /// The static list is the baseline: it must match regardless of casing or surrounding whitespace, because
    /// <c>Tenant.Name</c> is resolved case-insensitively against the host's first label and the controller only trims
    /// the custom domain, not the name.
    /// </summary>
    [TestMethod]
    public void IsReserved_Should_MatchEveryListedLabel_RegardlessOfCasingOrPadding()
    {
        foreach (var reservedName in ReservedTenantNames.Names)
        {
            Assert.IsTrue(ReservedTenantNames.IsReserved(reservedName), $"'{reservedName}' is in the list and must be reserved.");
            Assert.IsTrue(ReservedTenantNames.IsReserved(reservedName.ToUpperInvariant()), $"'{reservedName}' must be reserved case-insensitively.");
            Assert.IsTrue(ReservedTenantNames.IsReserved($"  {reservedName} "), $"'{reservedName}' must be reserved after trimming.");
        }

        Assert.IsFalse(ReservedTenantNames.IsReserved("acme"), "An ordinary tenant name must still be allowed.");
    }

    /// <summary>
    /// A blank name is the <c>[Required]</c> attribute's business. Returning true here would report "reserved name"
    /// for an empty form field, which is a confusing error for a different problem.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void IsReserved_Should_IgnoreABlankName(string? name)
    {
        Assert.IsFalse(ReservedTenantNames.IsReserved(name, "acme.myapp.com"));
    }

    /// <summary>
    /// The deployment's own sub domain label is reserved even though it is not in the static list - that is the whole
    /// point of passing the request's hosts in. Note the host handed to the guard may itself be a tenant's sub domain
    /// (the request can arrive on <c>acme.myapp.com</c>), which is why the first label of <b>every</b> supplied host
    /// counts.
    /// </summary>
    [TestMethod]
    [DataRow("acme", "acme.myapp.com", true)]
    [DataRow("ACME", "acme.myapp.com", true)]
    [DataRow("other", "acme.myapp.com", false)]
    [DataRow("myapp", "acme.myapp.com", false)]
    public void IsReserved_Should_ReserveTheFirstLabelOfEveryDeploymentHost(string name, string deploymentHost, bool expected)
    {
        Assert.AreEqual(expected, ReservedTenantNames.IsReserved(name, deploymentHost));
    }

    /// <summary>
    /// <b>The apex gate.</b> <c>TenantProvider</c> only resolves a tenant from the sub domain when the host has more
    /// than two labels (<c>host.Split('.') is { Length: &gt; 2 }</c>), so on <c>example.com</c> no sub domain lookup
    /// ever happens and the first label reserves nothing. Reserving it anyway would refuse the perfectly ordinary
    /// tenant name <c>example</c> on a deployment hosted at <c>example.com</c>, for no security gain.
    /// <para>
    /// Localhost development hits the same shape (<c>localhost</c> is a single label) - and <c>localhost</c> is in the
    /// static list anyway, which is where it belongs.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("example", "example.com")]
    [DataRow("myapp", "myapp.dev")]
    public void IsReserved_Should_NotReserveTheFirstLabelOfAnApexHost(string name, string apexHost)
    {
        Assert.IsFalse(ReservedTenantNames.IsReserved(name, apexHost),
            "An apex host performs no sub domain resolution, so its first label is claimable. This mirrors TenantProvider's `Length: > 2` gate exactly - widen one and the other must move too.");
    }

    /// <summary>
    /// A missing or blank host in the list is skipped rather than treated as a match; the hosts come from request
    /// state and configured trusted origins, either of which can legitimately be absent.
    /// </summary>
    [TestMethod]
    public void IsReserved_Should_SkipBlankDeploymentHosts()
    {
        Assert.IsFalse(ReservedTenantNames.IsReserved("acme", null, "", "   "));
        Assert.IsTrue(ReservedTenantNames.IsReserved("acme", null, "acme.myapp.com", ""),
            "A blank entry must not stop the real hosts from being considered.");
    }

    /// <summary>
    /// The custom-domain half. The deployment's own host is reserved, and so is <b>the zone above it</b> when the
    /// supplied host carries a sub domain label: the request may well arrive on a tenant's own host
    /// (<c>acme.myapp.com</c>), and taking that at face value would leave <c>myapp.com</c> - and every sibling
    /// tenant's host - claimable by anyone.
    /// </summary>
    [TestMethod]
    [DataRow("myapp.com", "myapp.com", true)]
    [DataRow("myapp.com", "acme.myapp.com", true)]
    [DataRow("other.myapp.com", "acme.myapp.com", true)]
    [DataRow("MyApp.Com", "acme.myapp.com", true)]
    [DataRow("acme.myapp.com", "acme.myapp.com", true)]
    public void IsReservedDomain_Should_ReserveTheDeploymentHostAndItsParentZone(string domain, string deploymentHost, bool expected)
    {
        Assert.AreEqual(expected, ReservedTenantNames.IsReservedDomain(domain, deploymentHost));
    }

    /// <summary>
    /// The suffix boundary. Matching on a bare <c>EndsWith</c> without the leading dot would let
    /// <c>evilmyapp.com</c> or <c>myapp.com.attacker.com</c> look like they belong to the deployment - here the risk
    /// runs the other way (a legitimate domain refused), but the same predicate is the one a widening "fix" would
    /// break in the dangerous direction.
    /// </summary>
    [TestMethod]
    [DataRow("notmyapp.com", "myapp.com")]
    [DataRow("myapp.com.attacker.com", "myapp.com")]
    [DataRow("myapp.competitor.com", "myapp.com")]
    [DataRow("acme.com", "myapp.com")]
    public void IsReservedDomain_Should_RequireAWholeLabelBoundary(string domain, string deploymentHost)
    {
        Assert.IsFalse(ReservedTenantNames.IsReservedDomain(domain, deploymentHost),
            $"'{domain}' is a different registrable domain and must remain usable as a tenant's custom domain.");
    }

    /// <summary>
    /// A <c>*</c> wildcard trusted origin (<c>https://*.myapp.com</c>, the entry that trusts every tenant sub domain)
    /// reserves its whole zone, since the deployment answers on all of it.
    /// </summary>
    [TestMethod]
    public void IsReservedDomain_Should_ReserveTheWholeZoneOfAWildcardHost()
    {
        Assert.IsTrue(ReservedTenantNames.IsReservedDomain("acme.myapp.com", "*.myapp.com"));
        Assert.IsTrue(ReservedTenantNames.IsReservedDomain("myapp.com", "*.myapp.com"));
        Assert.IsFalse(ReservedTenantNames.IsReservedDomain("acme.other.com", "*.myapp.com"));
    }

    /// <summary>
    /// A blank custom domain simply means "resolve me by sub domain", so it is not the domain guard's business.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void IsReservedDomain_Should_IgnoreABlankDomain(string? domain)
    {
        Assert.IsFalse(ReservedTenantNames.IsReservedDomain(domain, "myapp.com"));
    }
}
