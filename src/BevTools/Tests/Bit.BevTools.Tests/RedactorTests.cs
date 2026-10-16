
namespace Bit.BevTools.Tests;

[TestClass]
public class RedactorTests
{
    [TestMethod]
    [DataRow("Password", true)]
    [DataRow("accessToken", true)]
    [DataRow("X-Api-Key", true)]
    [DataRow("ConnectionString", true)]
    [DataRow("Authorization", true)]
    [DataRow("UserName", false)]
    [DataRow("Total", false)]
    public void Sensitive_names_are_matched_case_insensitively(string name, bool expected)
    {
        Assert.AreEqual(expected, BevRedactor.Default.IsSensitiveName(name));
    }

    [TestMethod]
    public void Query_string_values_with_sensitive_keys_are_redacted()
    {
        var url = "https://example.com/api/login?user=ada&token=abc123&page=2";
        var redacted = BevRedactor.Default.RedactUrl(url);
        Assert.AreEqual("https://example.com/api/login?user=ada&token=" + BevRedactor.RedactedValue + "&page=2", redacted);
        Assert.AreEqual("https://example.com/plain", BevRedactor.Default.RedactUrl("https://example.com/plain"));
    }

    [TestMethod]
    public void Jwt_values_are_redacted_even_under_innocent_query_names()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature";
        var redacted = BevRedactor.Default.RedactUrl("https://example.com/callback?session=" + jwt + "&page=2");

        Assert.AreEqual("https://example.com/callback?session=" + BevRedactor.RedactedValue + "&page=2", redacted);
        Assert.AreEqual("https://example.com/callback?auth=" + BevRedactor.RedactedValue,
            BevRedactor.Default.RedactUrl("https://example.com/callback?auth=Bearer+opaque"));
    }

    [TestMethod]
    public void Custom_patterns_are_honoured()
    {
        var redactor = new BevRedactor(["ssn", "iban"]);
        Assert.IsTrue(redactor.IsSensitiveName("CustomerIban"));
        Assert.IsFalse(redactor.IsSensitiveName("Password"));
    }
}
