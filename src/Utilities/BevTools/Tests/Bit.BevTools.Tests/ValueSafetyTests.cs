using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Bit.BevTools.Tests;

/// <summary>
/// What BevTools shows of a value: credential-shaped values are redacted whatever they are called, configuration is
/// never walked, and <see cref="BevValueAccess"/> limits how much of an application value is read at all.
/// </summary>
[TestClass]
public class ValueSafetyTests
{
    private const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.c2lnbmF0dXJl";

    [TestMethod]
    [DataRow(Jwt, true)]
    [DataRow("Bearer abc.def-ghi", true)]
    [DataRow("Server=db;Database=app;User Id=sa;Password=hunter2;", true)]
    [DataRow("DefaultEndpointsProtocol=https;AccountName=x;AccountKey=c2VjcmV0a2V5;", true)]
    [DataRow("{\"access_token\":\"opaque-value\"}", true)]
    [DataRow("-----BEGIN RSA PRIVATE KEY-----\nMIIE", true)]
    [DataRow("AKIAIOSFODNN7EXAMPLE", true)]
    [DataRow("ghp_aBcDeFgHiJkLmNoPqRsTuVwXyZ0123456789", true)]
    [DataRow("Ada Lovelace", false)]
    [DataRow("Use the password field below", false)]
    [DataRow("token: 5", false)]
    [DataRow("https://example.com/products?page=2", false)]
    public void Credentials_are_recognised_by_their_shape(string value, bool expected)
    {
        Assert.AreEqual(expected, BevRedactor.LooksLikeSecret(value));
    }

    [TestMethod]
    public void Free_text_keeps_what_is_not_a_credential()
    {
        Assert.AreEqual("Login failed for Server=db;Password=" + BevRedactor.RedactedValue + ";",
            BevRedactor.RedactText("Login failed for Server=db;Password=hunter2;"));
        Assert.AreEqual("invalid token " + BevRedactor.RedactedValue + " (expired)", BevRedactor.RedactText("invalid token " + Jwt + " (expired)"));
        Assert.AreEqual("Authorization: Bearer " + BevRedactor.RedactedValue, BevRedactor.RedactText("Authorization: Bearer abc.def"));

        const string plain = "Object reference not set to an instance of an object.";
        Assert.AreSame(plain, BevRedactor.RedactText(plain), "text without a credential is returned as it is");
    }

    [TestMethod]
    [DataRow("Server=db;Password='P@ss word';User Id=sa", "Server=db;Password='" + BevRedactor.RedactedValue + "';User Id=sa")]
    [DataRow("Server=db;Password='ab cd';User Id=sa", "Server=db;Password='" + BevRedactor.RedactedValue + "';User Id=sa")]
    [DataRow("Server=db;Pwd=\"a;bcdef\";User Id=sa", "Server=db;Pwd=\"" + BevRedactor.RedactedValue + "\";User Id=sa")]
    [DataRow("Server=db;Password='it''s';User Id=sa", "Server=db;Password='" + BevRedactor.RedactedValue + "';User Id=sa")]
    [DataRow("{\"password\":\"a\\\"b c\",\"user\":\"ada\"}", "{\"password\":\"" + BevRedactor.RedactedValue + "\",\"user\":\"ada\"}")]
    [DataRow("Server=db;Password='abc def", "Server=db;Password='" + BevRedactor.RedactedValue)]
    public void A_quoted_value_is_redacted_up_to_its_closing_quote(string text, string expected)
    {
        Assert.IsTrue(BevRedactor.LooksLikeSecret(text));
        Assert.AreEqual(expected, BevRedactor.RedactText(text));
        Assert.IsFalse(BevRedactor.LooksLikeSecret(expected));
    }

    [TestMethod]
    [DataRow("ey")]
    [DataRow("abcdefgh.ijklmn.")]
    [DataRow("eyabcde.")]
    public void Text_built_to_make_the_credential_patterns_backtrack_is_checked_in_linear_time(string unit)
    {
        var text = string.Concat(Enumerable.Repeat(unit, 100_000 / unit.Length));

        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        Assert.IsFalse(BevRedactor.LooksLikeSecret(text));
        Assert.AreSame(text, BevRedactor.RedactText(text));
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);

        Assert.IsTrue(elapsed.TotalMilliseconds < 1000, $"checking 100,000 characters took {elapsed.TotalMilliseconds:0} ms");
    }

    [TestMethod]
    [DataRow("https://app/cb?returnUrl=%2Fhome%3Fid_token%3D{0}&x=1")]
    [DataRow("{{\"payload\":\"{{\\u0022token\\u0022:\\u0022{0}\\u0022}}\"}}")]
    [DataRow("line one\\n{0}")]
    [DataRow("prefix{0} and {0}")]
    public void A_token_right_after_an_escape_or_other_characters_is_still_redacted(string format)
    {
        var text = string.Format(format, Jwt);

        Assert.IsTrue(BevRedactor.LooksLikeSecret(text));
        Assert.AreEqual(string.Format(format, BevRedactor.RedactedValue), BevRedactor.RedactText(text));
    }

    [TestMethod]
    public void A_credential_held_under_an_innocent_name_is_redacted()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using var _ = scope;

        var token = session.Inspector.Describe("Value", Jwt);
        var connection = session.Inspector.Describe("Target", "Server=db;Password=hunter2;");

        Assert.AreEqual(BevNodeKind.Redacted, token.Kind);
        Assert.AreEqual(BevRedactor.RedactedValue, token.Display);
        Assert.AreEqual(BevNodeKind.Redacted, connection.Kind);
        StringAssert.Contains(connection.Display, "Server=db;Password=" + BevRedactor.RedactedValue);
        Assert.IsFalse(connection.Display.Contains("hunter2", StringComparison.Ordinal));
    }

    [TestMethod]
    public void The_value_of_a_pair_with_a_sensitive_name_is_redacted()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using var _ = scope;
        var headers = new List<KeyValuePair<string, string>> { new("Accept", "json"), new("X-Api-Key", "opaque-123") };
        var claims = new[] { new Claim("access_token", "opaque-456"), new Claim("name", "Ada") };

        var headerItems = session.Inspector.Children(headers, "");
        var keyHeader = session.Inspector.Children(headers, "[1]");
        var claim = session.Inspector.Children(claims, "[0]");
        var flat = session.Inspector.Flatten(new { Headers = headers, Claims = claims });

        StringAssert.Contains(headerItems[1].Display, BevRedactor.RedactedValue, "KeyValuePair.ToString would print the value");
        Assert.AreEqual(BevRedactor.RedactedValue, keyHeader.Single(n => n.Name == "Value").Display);
        Assert.AreEqual(BevRedactor.RedactedValue, claim.Single(n => n.Name == "Value").Display);
        Assert.IsFalse(flat.Values.Any(v => v.Contains("opaque", StringComparison.Ordinal)), string.Join(", ", flat));
        Assert.IsTrue(flat.Values.Any(v => v.Contains("Ada", StringComparison.Ordinal)), "a pair with an ordinary name keeps its value");
    }

    [TestMethod]
    public void Configuration_and_options_are_never_walked()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using var _ = scope;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Smtp:Pass"] = "s3cret" }).Build();
        var options = Options.Create(new SmtpSettings { Host = "smtp.example.com", Pass = "s3cret" });

        var configNode = session.Inspector.Describe("Config", configuration);
        var optionsNode = session.Inspector.Describe("Smtp", options);
        var flat = session.Inspector.Flatten(new { Config = configuration, Smtp = options }, maxDepth: 6);

        Assert.IsFalse(configNode.IsExpandable);
        StringAssert.Contains(configNode.Display, "never read");
        Assert.IsFalse(optionsNode.IsExpandable);
        StringAssert.Contains(optionsNode.Display, "IOptions<SmtpSettings>");
        Assert.IsFalse(flat.Values.Any(v => v.Contains("s3cret", StringComparison.Ordinal)), string.Join(", ", flat));
    }

    [TestMethod]
    public void Exports_below_Full_keep_the_changes_a_render_captured_and_summarize_other_lists()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.ValueAccess = BevValueAccess.Shallow);
        using var _ = scope;
        var sample = new BevRenderSample { Sequence = 1, At = DateTimeOffset.UtcNow, StartTicks = System.Diagnostics.Stopwatch.GetTimestamp(), ChangedState = ["_counter: 0 → 1"] };
        session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Render, Category = "render", Title = "Counter", DurationMs = 1, Data = new RenderEventData(sample, 2) });
        session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.StateChange, Category = "orders", Title = "Order placed", Data = new Dictionary<string, object?> { ["items"] = new List<string> { "opaque-item" } } });

        foreach (var export in new[] { BevSessionExporter.ToJson(session), BevTraceExporter.ToChromeTrace(session) })
        {
            using var document = System.Text.Json.JsonDocument.Parse(export);
            var strings = StringsIn(document.RootElement).ToList();
            Assert.Contains(s => s.Contains("_counter: 0 → 1", StringComparison.Ordinal), strings, "the panel shows it under Shallow too");
            Assert.DoesNotContain(s => s.Contains("opaque-item", StringComparison.Ordinal), strings);
        }

        static IEnumerable<string> StringsIn(System.Text.Json.JsonElement element) => element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => [element.GetString()!],
            System.Text.Json.JsonValueKind.Object => element.EnumerateObject().SelectMany(p => StringsIn(p.Value)),
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().SelectMany(StringsIn),
            _ => [],
        };
    }

    [TestMethod]
    public void Shallow_access_shows_scalars_and_never_walks_or_prints_objects()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.ValueAccess = BevValueAccess.Shallow);
        using var _ = scope;
        var record = new Customer("Ada", 36);

        var name = session.Inspector.Describe("Name", "Ada");
        var customer = session.Inspector.Describe("Customer", record);
        var list = session.Inspector.Describe("Items", new List<int> { 1, 2, 3 });

        Assert.AreEqual("\"Ada\"", name.Display);
        Assert.IsFalse(customer.IsExpandable);
        Assert.AreEqual("Customer", customer.Display, "a record's ToString prints every member");
        Assert.IsFalse(list.IsExpandable);
        StringAssert.Contains(list.Display, "3 items");
        Assert.AreEqual(0, session.Inspector.Children(record, "").Count);
    }

    [TestMethod]
    public void No_access_reads_no_application_value_but_keeps_BevTools_metadata()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.ValueAccess = BevValueAccess.None);
        using var _ = scope;

        var name = session.Inspector.Describe("Name", "Ada");
        var count = session.Inspector.Describe("Count", 3);
        var cause = session.Inspector.DescribeMetadata("cause", "ParentRender");
        var bag = session.Inspector.DescribeMetadata("payload", new Customer("Ada", 36));

        Assert.AreEqual(BevRedactor.HiddenValue, name.Display);
        Assert.AreEqual(BevRedactor.HiddenValue, count.Display);
        Assert.AreEqual("string", name.TypeName, "the type is structure, not data");
        Assert.AreEqual("\"ParentRender\"", cause.Display);
        Assert.AreEqual("Customer", bag.Display, "an extension's object in an event is not printed either");

        var flat = new Dictionary<string, string>();
        session.Inspector.FlattenNamed(flat, "Customer", new Customer("Ada", 36), member: null, maxDepth: 3, maxItems: 25, maxEntries: 400, BevObjectInspector.FlattenDeadline());
        Assert.AreEqual(BevRedactor.HiddenValue, flat["Customer"]);
        Assert.HasCount(1, flat, "nothing below a value that is not read");
    }

    [TestMethod]
    public void No_access_the_app_chose_is_not_explained_as_the_multi_user_default()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.ValueAccess = BevValueAccess.None);
        using var _ = scope;

        var capability = CapabilityReport.Build(session).Single(c => c.Name == "Values (parameters, fields, state, claims)");
        var fact = BevRuntimeInfo.Build(session).SelectMany(s => s.Facts).Single(f => f.Name == "Value access");

        StringAssert.Contains(capability.Explanation, "as BitBevToolsOptions.ValueAccess sets it");
        Assert.IsFalse(capability.Explanation.Contains("serves many users", StringComparison.Ordinal), capability.Explanation);
        StringAssert.Contains(fact.Hint, "BitBevToolsOptions.ValueAccess is set to None");
        Assert.IsFalse(fact.Hint!.Contains("serves many users", StringComparison.Ordinal), fact.Hint);
    }

    [TestMethod]
    public void Error_messages_are_redacted_where_they_are_captured()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using var _ = scope;

        var record = session.Errors.Record(new InvalidOperationException("Cannot open Server=db;Password=hunter2;"), "test");

        Assert.IsFalse(record.Message.Contains("hunter2", StringComparison.Ordinal));
        StringAssert.Contains(record.Message, "Password=" + BevRedactor.RedactedValue);
        Assert.IsFalse(BevSessionExporter.ToJson(session).Contains("hunter2", StringComparison.Ordinal));
    }

    private sealed record Customer(string Name, int Age);

    private sealed class SmtpSettings
    {
        public string Host { get; set; } = "";

        public string Pass { get; set; } = "";
    }
}
