using Microsoft.Extensions.Logging;

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
        Assert.AreEqual("[email]", span.Attributes["other"], "Every string attribute is scrubbed, as the script scrubs them.");
        Assert.AreEqual("from [ip]", span.StatusMessage);
        Assert.AreEqual("https://x.test/?sig=REDACTED", span.Events[0].Attributes["http.url"]);

        var log = Records.Log("login by jane@example.com from 10.1.2.3");
        log.Attributes.Set("exception.stacktrace", "at X() Bearer abcdef");
        privacy.Apply(log);
        Assert.AreEqual("login by [email] from [ip]", log.Body);
        Assert.AreEqual("at X() Bearer [token]", log.Attributes["exception.stacktrace"]);
    }

    [TestMethod]
    public void ScrubsEveryStringAttribute_ButNotTheIdsOrTheCalledServer()
    {
        var log = Records.Log();
        log.Attributes.Set("Email", "jane@example.com")
            .Set("url.path", "/users/jane@example.com/orders")
            .Set("tags", new[] { "a@b.io", "plain" })
            .Set("count", 3)
            .Set("client.address", "10.1.2.3")
            .Set("server.address", "10.0.0.5")
            .Set("user_agent.original", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36")
            .Set("session.id", "4111111111111111")
            .Set("user.id", "jane@example.com")
            .Set("enduser.pseudo.id", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc");

        Create().Apply(log);

        Assert.AreEqual("[email]", log.Attributes["Email"]);
        Assert.AreEqual("/users/[email]/orders", log.Attributes["url.path"]);
        CollectionAssert.AreEqual(new[] { "[email]", "plain" }, (string[])log.Attributes["tags"]!);
        Assert.AreEqual(3L, log.Attributes["count"]);
        Assert.AreEqual("[ip]", log.Attributes["client.address"], "The user's address is personal data.");
        Assert.AreEqual("10.0.0.5", log.Attributes["server.address"], "The server that was called is not.");
        Assert.AreEqual("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36", log.Attributes["user_agent.original"], "Its four-part versions are not addresses.");
        Assert.AreEqual("4111111111111111", log.Attributes["session.id"]);
        Assert.AreEqual("jane@example.com", log.Attributes["user.id"], "An id the app set with UserTracking consent is kept as set.");
        Assert.AreEqual("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc", log.Attributes["enduser.pseudo.id"]);
    }

    [TestMethod]
    public void RedactsTheValuesOfSensitiveNames_WholeKeyOrLastSegment()
    {
        // The same expectations as privacy.test.js.
        var log = Records.Log();
        log.Attributes.Set("Password", "hunter2").Set("user.password", "x").Set("http.request.header.authorization", new[] { "Basic abc" })
            .Set("http.response.header.set-cookie", "a=b").Set("auth.token", 42).Set("password_policy", "strict").Set("author.name", "Jane")
            .Set("session.id", "kept");

        Create().Apply(log);

        foreach (var key in new[] { "Password", "user.password", "http.request.header.authorization", "http.response.header.set-cookie", "auth.token" })
        {
            Assert.AreEqual("REDACTED", log.Attributes[key], key);
        }
        Assert.AreEqual("strict", log.Attributes["password_policy"], "Only a whole name or segment matches.");
        Assert.AreEqual("Jane", log.Attributes["author.name"]);
        Assert.AreEqual("kept", log.Attributes["session.id"]);

        var prefixed = Records.Log();
        prefixed.Attributes.Set("http.request.header.x-tenant", "t").Set("other.header", "h");
        Create(o => o.RedactedAttributes.Add("http.request.header.*")).Apply(prefixed);
        Assert.AreEqual("REDACTED", prefixed.Attributes["http.request.header.x-tenant"]);
        Assert.AreEqual("h", prefixed.Attributes["other.header"]);
    }

    [TestMethod]
    public void VersionNamedKeys_KeepTheirFourPartValues_ButNothingElse()
    {
        // The same expectations as privacy.test.js.
        var log = Records.Log();
        log.Attributes.Set("AppVersion", "1.2.0.0").Set("app.version", "10.0.0.1").Set("build_version", "2.0.0.5").Set("Versions", new[] { "1.0.0.0" })
            .Set("conversion", "1.2.3.4").Set("api.version", "jane@example.com 1.2.3.4");

        Create().Apply(log);

        Assert.AreEqual("1.2.0.0", log.Attributes["AppVersion"]);
        Assert.AreEqual("10.0.0.1", log.Attributes["app.version"]);
        Assert.AreEqual("2.0.0.5", log.Attributes["build_version"]);
        CollectionAssert.AreEqual(new[] { "[ip]" }, (string[])log.Attributes["Versions"]!, "Only a key ending in the word.");
        Assert.AreEqual("[ip]", log.Attributes["conversion"]);
        Assert.AreEqual("[email] 1.2.3.4", log.Attributes["api.version"], "The other patterns still apply.");
        Assert.IsTrue(BelemetryPrivacy.IsVersionKey("os.Version"));
        Assert.IsFalse(BelemetryPrivacy.IsVersionKey("aversion"));
    }

    [TestMethod]
    public void ARedactedName_WinsOverTheHostExemption_AndTheIdsAreOnlyEverBlocked()
    {
        // The script's order: blocked keys go, then the ids are left alone by every other rule, then redaction, then
        // the scrubbing exemptions.
        var log = Records.Log();
        log.Attributes.Set("user_agent.original", "Mozilla/5.0").Set("server.address", "10.0.0.5").Set("session.id", "s1").Set("user.id", "u1");

        Create(o =>
        {
            o.RedactedAttributes.Add("user_agent.original");
            o.RedactedAttributes.Add("session.id");
            o.BlockedAttributes.Add("user.id");
        }).Apply(log);

        Assert.AreEqual("REDACTED", log.Attributes["user_agent.original"]);
        Assert.AreEqual("10.0.0.5", log.Attributes["server.address"]);
        Assert.AreEqual("s1", log.Attributes["session.id"], "Consent governs the ids, not the redaction rules.");
        Assert.IsFalse(log.Attributes.TryGetValue("user.id", out _), "Blocking one is the operator's explicit choice, and wins.");
    }

    [TestMethod]
    public async Task AStructuredLogPassword_IsRedacted()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp.Accounts");

        logger.LogInformation("Sign-in for {User} with {Password}", "jane", "hunter2");
        await host.FlushAsync();

        var log = host.Memory.Logs.Single();
        Assert.AreEqual("REDACTED", log.Attributes["Password"]);
        Assert.AreEqual("jane", log.Attributes["User"]);
        Assert.AreEqual("Sign-in for {User} with {Password}", log.Body, "The rendered message would carry the password.");
    }

    [TestMethod]
    public void AppliesToSpanLinks()
    {
        var privacy = Create(o => o.BlockedAttributes.Add("secret.attr"));
        var span = Records.Span();
        var link = new BelemetrySpanLink { TraceId = Records.LinkTraceId, SpanId = Records.LinkSpanId };
        link.Attributes.Set("password", "hunter2").Set("note", "jane@example.com").Set("secret.attr", "x");
        span.Links.Add(link);

        privacy.Apply(span);

        Assert.AreEqual("REDACTED", link.Attributes["password"]);
        Assert.AreEqual("[email]", link.Attributes["note"]);
        Assert.IsFalse(link.Attributes.Contains("secret.attr"));
    }

    [TestMethod]
    public void RedactsEveryUrlOfAnArray()
    {
        var attributes = new BelemetryAttributes()
            .Set("url.full", new[] { "https://x.test/a?token=abc", "https://x.test/b" })
            .Set("url.query", new[] { "code=1&page=2" });

        Create().ApplyToAttributes(attributes);

        CollectionAssert.AreEqual(new[] { "https://x.test/a?token=REDACTED", "https://x.test/b" }, (string[])attributes["url.full"]!);
        CollectionAssert.AreEqual(new[] { "code=REDACTED&page=2" }, (string[])attributes["url.query"]!);
    }

    [TestMethod]
    public void RedactsTheSignaturesOfPreSignedStorageUrls()
    {
        var privacy = Create();
        Assert.AreEqual("https://b.s3.test/o?X-Amz-Credential=REDACTED&X-Amz-Signature=REDACTED&X-Amz-Expires=60",
            privacy.RedactUrl("https://b.s3.test/o?X-Amz-Credential=AKIA&X-Amz-Signature=abc&X-Amz-Expires=60"));
        Assert.AreEqual("https://storage.test/o?X-Goog-Signature=REDACTED", privacy.RedactUrl("https://storage.test/o?X-Goog-Signature=abc"));
    }

    [TestMethod]
    public void ScrubsInTheScriptsOrder_AndCatchesUnsignedTokens()
    {
        var privacy = Create();
        Assert.AreEqual("Authorization: Bearer [token]", privacy.Scrub("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc"), "A bearer JWT is a bearer token, as in the script.");
        Assert.AreEqual("unsigned [jwt] end", privacy.Scrub("unsigned eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0. end"));

        var plain = "GET /api/orders";
        Assert.AreSame(plain, privacy.Scrub(plain), "A value no pattern matches comes back as is.");

        // The pre-checks' boundaries: the shortest address (4 digits, 3 dots) and a 13-digit card are still found.
        Assert.AreEqual("from [ip]", privacy.Scrub("from 1.2.3.4"));
        Assert.AreEqual("card [card]", privacy.Scrub("card 4222222222222"));
        Assert.AreEqual("/orders/42?page=2", privacy.Scrub("/orders/42?page=2"));

        var url = "https://api.example.com/orders/42";
        Assert.AreSame(url, privacy.RedactUrl(url), "A URL with nothing to redact is not rebuilt.");
        Assert.AreEqual("https://api.example.com/users/[email]", privacy.RedactUrl("https://api.example.com/users/jane@example.com"));
        Assert.AreEqual("https://api.example.com/hosts/[ip]", privacy.RedactUrl("https://api.example.com/hosts/10.1.2.3"));
    }

    [TestMethod]
    public void RedactsTheUrlQueryAttribute_AndDropsItWithTheQueryString()
    {
        var log = Records.Log();
        log.Attributes.Set("url.query", "token=abc&page=2&mail=jane@example.com");
        Create().Apply(log);
        Assert.AreEqual("token=REDACTED&page=2&mail=[email]", log.Attributes["url.query"]);

        var prefixed = Records.Log();
        prefixed.Attributes.Set("url.query", "?code=1");
        Create().Apply(prefixed);
        Assert.AreEqual("?code=REDACTED", prefixed.Attributes["url.query"]);

        var stripped = Records.Log();
        stripped.Attributes.Set("url.query", "a=1");
        Create(o => o.StripQueryString = true).Apply(stripped);
        Assert.IsFalse(stripped.Attributes.Contains("url.query"));
    }

    [TestMethod]
    public void RedactsTheQueryOfAFragmentRoute()
    {
        Assert.AreEqual("https://x.test/#/cb?access_token=REDACTED&state=1",
            Create(o => o.StripFragment = false).RedactUrl("https://x.test/#/cb?access_token=abc&state=1"));
    }

    [TestMethod]
    public async Task StructuredLogValues_AreScrubbedLikeTheMessage()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp.Accounts");

        logger.LogInformation("User {Email} signed in from {Address}", "jane@example.com", "10.1.2.3");
        await host.FlushAsync();

        var log = host.Memory.Logs.Single();
        Assert.AreEqual("User [email] signed in from [ip]", log.Body);
        Assert.AreEqual("[email]", log.Attributes["Email"]);
        Assert.AreEqual("[ip]", log.Attributes["Address"]);
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
