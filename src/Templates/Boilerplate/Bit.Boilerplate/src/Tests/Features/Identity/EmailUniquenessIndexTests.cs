using Microsoft.EntityFrameworkCore;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The database has to be the thing that stops two accounts from sharing one e-mail address, because nothing above it
/// does: <c>IdentityOptions.User.RequireUniqueEmail</c> is left at its framework default of <c>false</c>, so
/// <c>UserValidator</c> never looks, and the app-level guard is a check-then-act (<c>userManager.FindUser</c> in
/// <c>SignUp</c> / <c>SignIn</c> / <c>ExternalSignIn</c>, <c>EnsureIdentifierIsAvailable</c> in <c>ChangeEmail</c>) that
/// runs outside any transaction.
/// <para>
/// The index therefore has to sit on <c>NormalizedEmail</c> - the column every lookup uses - and not on the raw
/// <c>Email</c> column. On a case-sensitive collation, which is SQLite's default (BINARY) and PostgreSQL's, a raw-column
/// index accepts <c>victim@x.com</c> and <c>Victim@x.com</c> as two distinct values while
/// <c>UserStore.FindByEmailAsync</c> - a <c>SingleOrDefaultAsync</c> over <c>NormalizedEmail</c> - then throws
/// "Sequence contains more than one element" for that address forever, on sign-in, sign-up, password reset and
/// magic-link alike.
/// </para>
/// <para>
/// Asserted against the MODEL and the DDL the configured provider generates from it, rather than by inserting a
/// colliding row. Two reasons, both deliberate: the test database is the developer's real SQLite file and
/// <c>EnsureCreatedAsync</c> is a no-op once it exists, so a live insert would test whatever schema happened to be
/// created first rather than the one this configuration describes - and a test that writes a poisoned duplicate into a
/// database that outlives the run leaves exactly the corruption it exists to prevent.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class EmailUniquenessIndexTests
{
    [TestMethod]
    public async Task TheUserUniquenessIndex_Should_CoverTheColumnLookupsActuallyUse()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userEntity = dbContext.Model.FindEntityType(typeof(User))!;

        var uniqueIndexedColumnSets = userEntity.GetIndexes()
            .Where(index => index.IsUnique)
            .Select(index => index.Properties.Select(p => p.Name).ToArray())
            .ToArray();

        Assert.IsTrue(
            uniqueIndexedColumnSets.Any(columns => columns is [nameof(User.NormalizedEmail)]),
            "There must be a unique index on NormalizedEmail - the column UserStore.FindByEmailAsync queries with " +
            $"SingleOrDefaultAsync. Unique indexes found: [{string.Join(" | ", uniqueIndexedColumnSets.Select(c => string.Join(", ", c)))}].");

        Assert.IsFalse(
            uniqueIndexedColumnSets.Any(columns => columns is [nameof(User.Email)]),
            "A unique index on the RAW Email column is not a substitute: under a case-sensitive collation it accepts " +
            "two rows that share one NormalizedEmail, and every later lookup for that address then throws.");

        // The model is only half of it - the provider has to actually emit the constraint. This is the same check
        // `dotnet ef dbcontext script` performs, against the provider this configuration was generated with.
        var createScript = dbContext.Database.GenerateCreateScript();

        Assert.IsTrue(
            createScript.Contains("UNIQUE INDEX", StringComparison.OrdinalIgnoreCase)
            && createScript.Contains(nameof(User.NormalizedEmail), StringComparison.Ordinal),
            "The generated DDL carries no unique index over NormalizedEmail, so nothing enforces it at the database level.");

        // Note for whoever sees this go red after changing UserConfiguration: an EXISTING developer database is not
        // migrated by EnsureCreatedAsync, so the running app keeps the old index until the file is recreated or a
        // migration is applied. This test reads the model, so it is unaffected by that - which is the point.
    }

    public TestContext TestContext { get; set; } = default!;
}
