using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Products;

namespace Boilerplate.Tests.E2E.Features.OData;

/// <summary>
/// Sales' <c>ProductViewController.Get</c> - anonymous, <c>[EnableQuery]</c> over <c>DbContext.Products.Project()</c>
/// - asked real OData queries over http and answered against the very same database: filters, sorting, paging and
/// <c>$select</c>, each compared row for row with the equivalent LINQ over the host's store (the fallback tenant).
/// <para>
/// Every request carries a query key of its own: the list is cached for five minutes per full url (QueryKeys = "*"),
/// and this is about what OData answers, not what a cache kept.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class ODataProductViewTests
{
    private static readonly Uri productViewGet = new(new Uri(DeployedApps.Sales), "api/v1/ProductView/Get");

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Sorted to the end by ShortId on both sides wherever the requested order has ties, so the two lists can only
    /// differ by what OData did, never by the database's arbitrary tie order.
    /// </summary>
    [TestMethod]
    [DataRow("price-filter-sorted", DisplayName = "$filter Price gt, $orderby Price desc")]
    [DataRow("category-filter", DisplayName = "$filter CategoryName eq")]
    [DataRow("contains", DisplayName = "$filter contains(Name)")]
    [DataRow("range", DisplayName = "$filter Price ge and le")]
    [DataRow("paging", DisplayName = "$orderby Name, $skip, $top")]
    [DataRow("combined", DisplayName = "$filter + $orderby + $skip + $top")]
    public async Task ProductViewGet_Should_AnswerAsTheDatabaseDoes(string caseName)
    {
        var (odata, expected) = caseName switch
        {
            "price-filter-sorted" => ("$filter=Price gt 60000&$orderby=Price desc,ShortId",
                (Func<IQueryable<Product>, IQueryable<Product>>)(q => q.Where(p => p.Price > 60000).OrderByDescending(p => p.Price).ThenBy(p => p.ShortId))),
            "category-filter" => ("$filter=CategoryName eq 'BMW'&$orderby=Name,ShortId",
                q => q.Where(p => p.Category!.Name == "BMW").OrderBy(p => p.Name).ThenBy(p => p.ShortId)),
            "contains" => ("$filter=contains(Name,'SUV')&$orderby=ShortId",
                q => q.Where(p => p.Name!.Contains("SUV")).OrderBy(p => p.ShortId)),
            "range" => ("$filter=Price ge 40000 and Price le 80000&$orderby=Price,ShortId",
                q => q.Where(p => p.Price >= 40000 && p.Price <= 80000).OrderBy(p => p.Price).ThenBy(p => p.ShortId)),
            "paging" => ("$orderby=Name,ShortId&$skip=10&$top=5",
                q => q.OrderBy(p => p.Name).ThenBy(p => p.ShortId).Skip(10).Take(5)),
            "combined" => ("$filter=Price ge 40000&$orderby=Price desc,ShortId&$skip=2&$top=4",
                q => q.Where(p => p.Price >= 40000).OrderByDescending(p => p.Price).ThenBy(p => p.ShortId).Skip(2).Take(4)),
            _ => throw new ArgumentOutOfRangeException(nameof(caseName), caseName, null)
        };

        var answered = (await Query(odata)).Select(item => ValueOf(item, "shortId")!.GetValue<int>()).ToArray();

        await using var dbContext = await CreateDbContext();
        var fromDatabase = await expected(StoreProducts(dbContext)).Select(p => p.ShortId).ToArrayAsync(TestContext.CancellationToken);

        Assert.IsNotEmpty(fromDatabase, $"'{odata}' matches no product in the database, so the comparison would pass on two empty lists.");
        Assert.AreSequenceEqual(fromDatabase, answered,
            $"'{odata}' answered [{string.Join(", ", answered)}], the database says [{string.Join(", ", fromDatabase)}].");
    }

    /// <summary>Only the selected properties come back, with the database's values.</summary>
    [TestMethod]
    public async Task Select_Should_ReturnOnlyTheSelectedProperties()
    {
        var answered = await Query("$select=ShortId,Name&$orderby=ShortId&$top=5");

        await using var dbContext = await CreateDbContext();
        var fromDatabase = await StoreProducts(dbContext).OrderBy(p => p.ShortId).Take(5)
            .Select(p => new { p.ShortId, p.Name })
            .ToArrayAsync(TestContext.CancellationToken);

        Assert.HasCount(fromDatabase.Length, answered);

        foreach (var (item, expected) in answered.Zip(fromDatabase))
        {
            var keys = item!.AsObject().Select(pair => pair.Key.ToLowerInvariant()).Order().ToArray();
            Assert.AreSequenceEqual(new[] { "name", "shortid" }, keys, $"$select=ShortId,Name returned {item.ToJsonString()}.");

            Assert.AreEqual(expected.ShortId, ValueOf(item, "shortId")!.GetValue<int>());
            Assert.AreEqual(expected.Name, ValueOf(item, "name")!.GetValue<string>());
        }
    }

    /// <summary>A malformed query is the caller's mistake: a 400, not a 500 and not a silently ignored filter.</summary>
    [TestMethod]
    public async Task AFilterOnAPropertyThatDoesNotExist_Should_BeRefusedAsABadRequest()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync(Url("$filter=NoSuchProperty eq 1"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private async Task<JsonArray> Query(string odata)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync(Url(odata), TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"'{odata}' was answered {(int)response.StatusCode}: {body}");

        return JsonNode.Parse(body)!.AsArray();
    }

    /// <summary>A query key no one else uses, so the answer comes from the api rather than the edge.</summary>
    private static Uri Url(string odata) => new($"{productViewGet}?{odata}&e2e={Guid.NewGuid():N}");

    /// <summary>
    /// Case-insensitive: a plain answer is camelCase, but a $select'ed one comes back with the CLR names (PascalCase).
    /// </summary>
    private static JsonNode? ValueOf(JsonNode? item, string name)
    {
        return item!.AsObject().FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>The anonymous caller's store is the host's: the fallback tenant, which is what the api filters to.</summary>
    private static IQueryable<Product> StoreProducts(AppDbContext dbContext)
    {
        return dbContext.Products.IgnoreQueryFilters().Where(p => p.TenantId == TenantConfiguration.FallbackTenantId);
    }

    private async Task<AppDbContext> CreateDbContext()
    {
        await using (var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi))
        {
            var configuration = apiClient.Services.GetRequiredService<IConfiguration>();

            if (string.IsNullOrWhiteSpace(configuration["GlobalAdminEmail"]) || string.IsNullOrWhiteSpace(configuration["GlobalAdminPassword"]))
                Assert.Inconclusive("'GlobalAdminEmail' / 'GlobalAdminPassword' are not in this project's user secrets, and the database is reached through the global admin.");
        }

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        return await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);
    }
}
