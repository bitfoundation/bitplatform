//+:cnd:noEmit
using System.Text.Json.Nodes;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Categories;
//#endif

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpQueryTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task QueryEntity_Should_RequireProjection_AndRefuseCredentials()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var missing = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = Array.Empty<string>()
        }, TestContext.CancellationToken);
        Assert.Contains("projection", missing, StringComparison.OrdinalIgnoreCase);

        var hash = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id", "PasswordHash" }
        }, TestContext.CancellationToken);
        Assert.Contains("forbidden", hash, StringComparison.OrdinalIgnoreCase);

        var filter = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id", "Email" },
            ["filter"] = "PasswordHash != null"
        }, TestContext.CancellationToken);
        Assert.Contains("forbidden", filter, StringComparison.OrdinalIgnoreCase);

        var constructed = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id", "Email" },
            ["filter"] = "new(Email) != null"
        }, TestContext.CancellationToken);
        Assert.Contains("new", constructed, StringComparison.OrdinalIgnoreCase);

        // Dynamic LINQ names the row itself "it"/"this"/"root"/"parent", which is the same column under another name.
        foreach (var selfReference in new[] { "it", "this", "root", "parent" })
        {
            var aliased = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
            {
                ["entity"] = "User",
                ["select"] = new[] { "Id", "Email" },
                ["filter"] = $"{selfReference}.PasswordHash != null"
            }, TestContext.CancellationToken);
            Assert.Contains("forbidden", aliased, StringComparison.OrdinalIgnoreCase,
                $"'{selfReference}.PasswordHash' reads the password hash, so it must be refused exactly like 'PasswordHash'. Result: {aliased}");
        }
    }

    [TestMethod]
    public async Task QueryEntity_Should_ReturnProjectedRows_AndCapTake()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var text = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id", "Email" },
            ["take"] = 5
        }, TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;

        Assert.AreEqual("User", json["entity"]!.GetValue<string>());
        Assert.IsGreaterThan(0, json["count"]!.GetValue<int>());
        Assert.IsTrue(json["rows"]![0]!["Email"] is not null || json["rows"]![0]!["email"] is not null);

        var payload = text.ToLowerInvariant();
        Assert.DoesNotContain("passwordhash", payload);
        Assert.DoesNotContain("securitystamp", payload);

        var capped = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id" },
            ["take"] = 10_000
        }, TestContext.CancellationToken);
        var cappedJson = JsonNode.Parse(capped)!;
        Assert.IsLessThanOrEqualTo(100, cappedJson["take"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task QueryEntity_Should_RefuseUnknownAndCredentialEntities_ButAllowHangfireStorage()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var unknown = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "NotAnEntity",
            ["select"] = new[] { "Id" }
        }, TestContext.CancellationToken);
        Assert.Contains("Unknown entity", unknown);

        foreach (var entity in new[] { "WebAuthnCredential", "UserToken", "DataProtectionKey" })
        {
            var refused = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
            {
                ["entity"] = entity,
                ["select"] = new[] { "Id" }
            }, TestContext.CancellationToken);
            Assert.Contains("credential", refused, StringComparison.OrdinalIgnoreCase,
                $"{entity} must be refused as a credential-shaped entity. Result: {refused}");
        }

        var schema = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetDatabaseSchema", new() { ["entityName"] = (string?)null }, TestContext.CancellationToken))!;
        var hangfireEntity = schema["entities"]!.AsArray()
            .FirstOrDefault(entity => entity!["hangfireStorage"]?.GetValue<bool>() is true)
            ?["entity"]?.GetValue<string>();
        Assert.IsFalse(string.IsNullOrWhiteSpace(hangfireEntity), "The EF model includes Hangfire's jobs schema.");

        // Refusing these protected nothing: the Hangfire tools hand the same caller the job arguments themselves.
        var hangfire = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = hangfireEntity,
            ["select"] = new[] { "Id" }
        }, TestContext.CancellationToken))!;
        Assert.IsNull(hangfire["error"], $"Hangfire's own tables are queryable. Result: {hangfire}");
        Assert.AreEqual(hangfireEntity, hangfire["entity"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task QueryEntity_Should_RefuseForbiddenOrderBy_AndHonorAFilter()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        foreach (var key in new[] { "PasswordHash desc", "it.PasswordHash desc" })
        {
            var orderBy = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
            {
                ["entity"] = "User",
                ["select"] = new[] { "Id", "Email" },
                ["orderBy"] = key
            }, TestContext.CancellationToken);
            Assert.Contains("forbidden", orderBy, StringComparison.OrdinalIgnoreCase,
                $"Ordering by '{key}' leaks the hash through the row order. Result: {orderBy}");
        }

        var text = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Email" },
            ["filter"] = $"Email == \"{email}\""
        }, TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;
        Assert.AreEqual(1, json["count"]!.GetValue<int>(),
            "A filter on Email must return that one account, not the whole Users table.");
        var returned = (json["rows"]![0]!["Email"] ?? json["rows"]![0]!["email"])!.GetValue<string>();
        Assert.AreEqual(email, returned);
    }

    //#if (multitenant == true)
    /// <summary>
    /// A global admin has no tenant selected, so a tenant-filtered read would answer for the fallback tenant or for
    /// nothing - and an external sign-in gives her no way to switch tenant. A diagnostic tool that quietly hides rows
    /// is worse than useless, so QueryEntity reads with IgnoreQueryFilters.
    /// </summary>
    [TestMethod]
    public async Task QueryEntity_Should_IgnoreTheTenantQueryFilter()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var hiddenName = $"hidden-{Guid.NewGuid():N}";
        var otherTenantId = Guid.CreateVersion7();

        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Tenants.AddAsync(new Tenant
            {
                Id = otherTenantId,
                Name = $"t{Guid.NewGuid():N}"[..20],
                Title = "Other",
                IsActive = true,
                Version = 0
            }, TestContext.CancellationToken);
            await db.Categories.AddAsync(new Category
            {
                Id = Guid.CreateVersion7(),
                Name = hiddenName,
                Color = "#000000",
                Version = 0,
                TenantId = otherTenantId
            }, TestContext.CancellationToken);
            await db.SaveChangesAsync(TestContext.CancellationToken);
        }

        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);
        var text = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "Category",
            ["select"] = new[] { "Name", "TenantId" },
            ["filter"] = $"Name == \"{hiddenName}\"",
            ["take"] = 100
        }, TestContext.CancellationToken);

        Assert.Contains(hiddenName, text,
            "A Category belonging to a tenant the caller is not in must still be returned; the tenant filter is what " +
            "would otherwise hide every row from a global admin who has no tenant selected.");

        Assert.Contains(otherTenantId.ToString(), text, StringComparison.OrdinalIgnoreCase,
            "The row must carry the other tenant's id, so the caller can see which tenant it came from.");
    }
    //#endif
}
