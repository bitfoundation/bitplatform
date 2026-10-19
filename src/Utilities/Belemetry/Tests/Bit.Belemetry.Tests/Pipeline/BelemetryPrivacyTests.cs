namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryPrivacyTests
{
    private static BelemetryPrivacy Create(Action<BelemetryPrivacyOptions>? configure = null)
    {
        var options = new BelemetryPrivacyOptions();
        configure?.Invoke(options);
        return new BelemetryPrivacy(options);
    }

    [TestMethod]
    public void RedactsSensitiveQueryParameters_CaseInsensitively()
    {
        var privacy = Create();
        Assert.AreEqual("https://x.test/p?TOKEN=REDACTED&page=2&api_key=REDACTED&flag",
            privacy.RedactUrl("https://x.test/p?TOKEN=abc&page=2&api_key=s3cr3t&flag"));
        Assert.AreEqual("https://x.test/p?access%5Ftoken=REDACTED", privacy.RedactUrl("https://x.test/p?access%5Ftoken=abc"));
    }

    [TestMethod]
    public void StripsFragmentByDefault_AndRedactsTokenFragmentsWhenKept()
    {
        Assert.AreEqual("https://x.test/cb", Create().RedactUrl("https://x.test/cb#access_token=abc"));
        Assert.AreEqual("https://x.test/cb#access_token=REDACTED&state=1",
            Create(o => o.StripFragment = false).RedactUrl("https://x.test/cb#access_token=abc&state=1"));
        Assert.AreEqual("https://x.test/cb#section", Create(o => o.StripFragment = false).RedactUrl("https://x.test/cb#section"));
    }

    [TestMethod]
    public void StripsQueryString()
    {
        Assert.AreEqual("https://x.test/p", Create(o => o.StripQueryString = true).RedactUrl("https://x.test/p?a=1#f"));
    }

    [TestMethod]
    public void ScrubsPatterns()
    {
        var privacy = Create();
        Assert.AreEqual("contact [email] now", privacy.Scrub("contact jane.doe+x@example.co.uk now"));
        Assert.AreEqual("token [jwt] end", privacy.Scrub("token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc-_DEF end"));
        Assert.AreEqual("Authorization: Bearer [token]", privacy.Scrub("Authorization: Bearer abc.DEF-123~+/=="));
        Assert.AreEqual("card [card] ok", privacy.Scrub("card 4111 1111 1111 1111 ok"));
        Assert.AreEqual("card [card]", privacy.Scrub("card 4111-1111-1111-1111"));
        Assert.AreEqual("from [ip]:80", privacy.Scrub("from 192.168.10.1:80"));
    }

    [TestMethod]
    public void LeavesNonMatchesAlone()
    {
        var privacy = Create();
        Assert.AreEqual("order 4111111111111112 failed", privacy.Scrub("order 4111111111111112 failed"), "Not Luhn-valid.");
        Assert.AreEqual("System.Private.CoreLib, Version=10.0.0.0, Culture=neutral", privacy.Scrub("System.Private.CoreLib, Version=10.0.0.0, Culture=neutral"));
        Assert.AreEqual("version 1.2.3 released", privacy.Scrub("version 1.2.3 released"));
        Assert.AreEqual("a@b", privacy.Scrub("a@b"));
        Assert.AreEqual("jane@example.com", Create(o => o.ScrubPatterns = false).Scrub("jane@example.com"));
    }

    [TestMethod]
    public void Luhn()
    {
        Assert.IsTrue(BelemetryPrivacy.IsLuhnValid("4111111111111111"));
        Assert.IsTrue(BelemetryPrivacy.IsLuhnValid("5500 0000 0000 0004"));
        Assert.IsFalse(BelemetryPrivacy.IsLuhnValid("4111111111111112"));
        Assert.IsFalse(BelemetryPrivacy.IsLuhnValid("0000"), "Too short.");
        Assert.IsFalse(BelemetryPrivacy.IsLuhnValid("41111111a1111111"));
    }

    [TestMethod]
    public void AppliesToSpansAndLogs()
    {
        var privacy = Create(o => o.BlockedAttributes.Add("secret.attr"));

        var span = Records.Span(name: "GET https://x.test/a?password=p");
        span.Attributes.Set("url.full", "https://x.test/a?code=1#frag").Set("page.referrer", "https://r.test/?email=a").Set("secret.attr", "x")
            .Set("exception.message", "user jane@example.com failed").Set("other", "jane@example.com");
        span.StatusMessage = "from 10.0.0.1";
        span.Events.Add(new BelemetrySpanEvent { Name = "e", Attributes = new BelemetryAttributes().Set("http.url", "https://x.test/?sig=abc") });

        privacy.Apply(span);

        Assert.AreEqual("GET https://x.test/a?password=REDACTED", span.Name);
        Assert.AreEqual("https://x.test/a?code=REDACTED", span.Attributes["url.full"]);
        Assert.AreEqual("https://r.test/?email=REDACTED", span.Attributes["page.referrer"]);
        Assert.IsFalse(span.Attributes.Contains("secret.attr"));
        Assert.AreEqual("user [email] failed", span.Attributes["exception.message"]);
        Assert.AreEqual("jane@example.com", span.Attributes["other"], "Only known text attributes are scrubbed.");
        Assert.AreEqual("from [ip]", span.StatusMessage);
        Assert.AreEqual("https://x.test/?sig=REDACTED", span.Events[0].Attributes["http.url"]);

        var log = Records.Log("login by jane@example.com from 10.1.2.3");
        log.Attributes.Set("exception.stacktrace", "at X() Bearer abcdef");
        privacy.Apply(log);
        Assert.AreEqual("login by [email] from [ip]", log.Body);
        Assert.AreEqual("at X() Bearer [token]", log.Attributes["exception.stacktrace"]);
    }

    [TestMethod]
    public void Urls_KeepTheirHost_AndLoseTheirCredentials()
    {
        var privacy = Create();

        Assert.AreEqual("http://10.0.0.5:5000/orders/[ip]?email=REDACTED", privacy.RedactUrl("http://10.0.0.5:5000/orders/10.1.2.3?email=a@b.io"));
        Assert.AreEqual("https://x.test/a/[email]", privacy.RedactUrl("https://user:p%40ss@x.test/a/jane@example.com"));
        Assert.AreEqual("http://192.168.1.1", privacy.RedactUrl("http://192.168.1.1"));
        Assert.AreEqual("from [ip]?a=1", privacy.RedactUrl("from 10.0.0.1?a=1"), "Not a URL: scrubbed whole.");

        var span = Records.Span();
        span.Attributes.Set("url.full", "http://10.0.0.5:8080/api").Set("server.address", "10.0.0.5");
        privacy.Apply(span);
        Assert.AreEqual("http://10.0.0.5:8080/api", span.Attributes["url.full"]);
        Assert.AreEqual("10.0.0.5", span.Attributes["server.address"]);
    }
}
