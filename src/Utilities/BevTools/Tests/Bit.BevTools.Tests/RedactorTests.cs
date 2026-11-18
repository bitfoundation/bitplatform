
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
        Assert.AreEqual(expected, BevToolsRedactor.Default.IsSensitiveName(name));
    }

    [TestMethod]
    public void Query_string_values_with_sensitive_keys_are_redacted()
    {
        var url = "https://example.com/api/login?user=ada&token=abc123&page=2";
        var redacted = BevToolsRedactor.Default.RedactUrl(url);
        Assert.AreEqual("https://example.com/api/login?user=ada&token=" + BevToolsRedactor.RedactedValue + "&page=2", redacted);
        Assert.AreEqual("https://example.com/plain", BevToolsRedactor.Default.RedactUrl("https://example.com/plain"));
    }

    [TestMethod]
    public void Fragment_parameters_and_relative_urls_are_redacted_too()
    {
        // OAuth implicit and hybrid flows return tokens in the fragment, and navigation targets are base-relative.
        Assert.AreEqual("signin#access_token=" + BevToolsRedactor.RedactedValue + "&state=xyz",
            BevToolsRedactor.Default.RedactUrl("signin#access_token=opaque&state=xyz"));
        Assert.AreEqual("reset-password?token=" + BevToolsRedactor.RedactedValue + "&step=2#top",
            BevToolsRedactor.Default.RedactUrl("reset-password?token=abc123&step=2#top"));
        Assert.AreEqual("docs#section-2", BevToolsRedactor.Default.RedactUrl("docs#section-2"));
    }

    [TestMethod]
    public void Jwt_values_are_redacted_even_under_innocent_query_names()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature";
        var redacted = BevToolsRedactor.Default.RedactUrl("https://example.com/callback?session=" + jwt + "&page=2");

        Assert.AreEqual("https://example.com/callback?session=" + BevToolsRedactor.RedactedValue + "&page=2", redacted);
        Assert.AreEqual("https://example.com/callback?auth=" + BevToolsRedactor.RedactedValue,
            BevToolsRedactor.Default.RedactUrl("https://example.com/callback?auth=Bearer+opaque"));
    }

    [TestMethod]
    public void Custom_patterns_are_honoured()
    {
        var redactor = new BevToolsRedactor(["ssn", "iban"]);
        Assert.IsTrue(redactor.IsSensitiveName("CustomerIban"));
        Assert.IsFalse(redactor.IsSensitiveName("Password"));
    }
}
