using Boilerplate.Server.Api.Infrastructure.DevMcp;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("UnitTest")]
public class DevMcpForbiddenColumnsTests
{
    [TestMethod]
    [DataRow("User", "PasswordHash")]
    [DataRow("User", "SecurityStamp")]
    [DataRow("User", "ConcurrencyStamp")]
    [DataRow("UserLogin", "ProviderKey")]
    [DataRow("UserToken", "Value")]
    [DataRow("WebAuthnCredential", "PublicKey")]
    [DataRow("DataProtectionKey", "Xml")]
    public void CredentialShapedNames_Should_BeRejected(string entity, string property)
    {
        Assert.IsTrue(DevMcpForbiddenColumns.LooksLikeCredential(entity, property),
            $"{entity}.{property} must be treated as a credential.");
    }

    [TestMethod]
    [DataRow("User", "Email")]
    [DataRow("User", "UserName")]
    [DataRow("User", "EmailTokenRequestedOn")]
    [DataRow("User", "Id")]
    [DataRow("Product", "Name")]
    public void OrdinaryColumns_Should_BeAllowed(string entity, string property)
    {
        Assert.IsFalse(DevMcpForbiddenColumns.LooksLikeCredential(entity, property),
            $"{entity}.{property} is not a credential.");
    }

    [TestMethod]
    public void FilterExpressions_Should_ExtractPropertyPaths_AndSkipLiterals()
    {
        var paths = DevMcpQueryGuards.ExtractPaths("""Email == "PasswordHash" && UserName != null""").ToArray();
        Assert.Contains("Email", paths);
        Assert.Contains("UserName", paths);
        Assert.DoesNotContain("PasswordHash", paths);
    }

    [TestMethod]
    [DataRow("it.PasswordHash")]
    [DataRow("this.PasswordHash")]
    [DataRow("root.PasswordHash")]
    [DataRow("parent.PasswordHash")]
    [DataRow("it.it.PasswordHash")]
    public void ARowSelfReference_Should_NotHideTheColumnItNames(string path)
    {
        Assert.Contains("PasswordHash", DevMcpQueryGuards.ExtractPaths($"{path} != null").ToArray(),
            $"'{path}' is 'PasswordHash'. Skipping the whole path over its prefix would hand back the column the guard exists to refuse.");
    }

    /// <summary>
    /// Verified against real servers: SQL Server 2025 answers "Incorrect syntax near the keyword 'READ'" and MySQL 8
    /// answers error 1568 "Transaction characteristics can't be changed while a transaction is in progress". Issuing it
    /// on either breaks every Dev MCP read there, and only that one CI job would notice.
    /// </summary>
    [TestMethod]
    [DataRow("Npgsql.EntityFrameworkCore.PostgreSQL", true)]
    [DataRow("Pomelo.EntityFrameworkCore.MySql", false)]
    [DataRow("Microsoft.EntityFrameworkCore.SqlServer", false)]
    [DataRow("Microsoft.EntityFrameworkCore.Sqlite", false)]
    [DataRow(null, false)]
    public void AReadOnlyTransaction_Should_OnlyBeOpenedOnProvidersThatHaveOne(string? providerName, bool supported)
    {
        Assert.AreEqual(supported, DevMcpReadOnly.SupportsReadOnlyTransaction(providerName));
    }

    [TestMethod]
    public void ATypeNameOrLiteral_Should_StillBeSkipped()
    {
        var paths = DevMcpQueryGuards.ExtractPaths("CreatedOn > DateTime(2024,1,1) && IsActive == true").ToArray();
        Assert.Contains("CreatedOn", paths);
        Assert.Contains("IsActive", paths);
        Assert.DoesNotContain("DateTime", paths);
        Assert.DoesNotContain("true", paths);
    }
}
