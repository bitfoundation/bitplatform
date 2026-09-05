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
}
