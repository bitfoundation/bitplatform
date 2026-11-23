
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
    [DataRow("otp", true)]
    [DataRow("Otp", true)]
    [DataRow("UserOtp", true)]
    [DataRow("OTPCode", true)]
    [DataRow("IsOTPValid", true)]
    [DataRow("otp_code", true)]
    [DataRow("x-otp", true)]
    [DataRow("TotpSecret", true)]
    [DataRow("dbpwd", true)]
    [DataRow("UserSsn", true)]
    [DataRow("NotPublished", false)]
    [DataRow("IsNotPrimary", false)]
    [DataRow("HotPath", false)]
    [DataRow("Footprint", false)]
    [DataRow("hotplug", false)]
    [DataRow("HOTPATH", false)]
    [DataRow("ClassName", false)]
    [DataRow("CssClassName", false)]
    public void Short_patterns_match_where_they_start_or_end_a_word(string name, bool expected)
    {
        Assert.AreEqual(expected, BevToolsRedactor.Default.IsSensitiveName(name), name);
    }

    [TestMethod]
    public void One_time_passwords_are_redacted_in_urls_and_text()
    {
        Assert.AreEqual("confirm?email=ada%40example.com&otp=" + BevToolsRedactor.RedactedValue,
            BevToolsRedactor.Default.RedactUrl("confirm?email=ada%40example.com&otp=482913"));
        StringAssert.DoesNotMatch(BevToolsRedactor.RedactText("Navigating to /confirm?otp=482913"), new System.Text.RegularExpressions.Regex("482913"));
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
