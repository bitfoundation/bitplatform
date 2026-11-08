namespace Bit.Belemetry.Tests.Pipeline;

/// <summary>The C# side of <c>Tests/bit-belemetry-js/routes.test.js</c>: the same cases, the same answers.</summary>
[TestClass]
public class BelemetryPageRouteMatcherTests
{
    private static string Match(string[] routes, string path, string basePath = "/") => new BelemetryPageRouteMatcher(routes).Template(path, basePath);

    [TestMethod]
    public void PrefersLiterals_ToConstrainedParameters_ToParameters_ToCatchAlls()
    {
        string[] routes = ["/{*path}", "/orders/{id}", "/orders/{id:int}", "/orders/new", "/orders/{**rest:nonfile}", "/orders"];
        CollectionAssert.AreEqual(new[] { "/orders", "/orders/new", "/orders/{id:int}", "/orders/{id}", "/orders/{**rest:nonfile}", "/{*path}" },
                                  new BelemetryPageRouteMatcher(routes).Templates.ToArray());
        Assert.AreEqual("/orders", Match(routes, "/orders"));
        Assert.AreEqual("/orders/new", Match(routes, "/orders/new"));
        Assert.AreEqual("/orders/{id:int}", Match(routes, "/orders/42"));
        Assert.AreEqual("/orders/{id}", Match(routes, "/orders/abc"));
        Assert.AreEqual("/orders/{**rest:nonfile}", Match(routes, "/orders/a/b"));
        Assert.AreEqual("/{*path}", Match(routes, "/orders/a/b.txt"));
        Assert.AreEqual("/{*path}", Match(routes, "/"));
    }

    [TestMethod]
    public void MatchesCaseInsensitively_DecodesSegments_AndIgnoresEmptyOnes()
    {
        string[] routes = ["/Users/{name}/Profile"];
        Assert.AreEqual("/Users/{name}/Profile", Match(routes, "/users/jane%20doe/PROFILE/"));
        Assert.AreEqual("/Users/{name}/Profile", Match(routes, "//users//jdoe//profile"));
        Assert.AreEqual("/users/jdoe", Match(routes, "/users/jdoe"));
    }

    [TestMethod]
    public void LetsOptionalParameters_Defaults_AndCatchAllsBeLeftOut()
    {
        string[] routes = ["/search/{term?}", "/page/{n:int=1}", "/docs/{*slug}"];
        Assert.AreEqual("/search/{term?}", Match(routes, "/search"));
        Assert.AreEqual("/search/{term?}", Match(routes, "/search/shoes"));
        Assert.AreEqual("/search/shoes/red", Match(routes, "/search/shoes/red"));
        Assert.AreEqual("/page/{n:int=1}", Match(routes, "/page"));
        Assert.AreEqual("/page/x", Match(routes, "/page/x"));
        Assert.AreEqual("/docs/{*slug}", Match(routes, "/docs"));
        Assert.AreEqual("/docs/{*slug}", Match(routes, "/docs/a/b/c"));
    }

    [TestMethod]
    public void ChecksTheConstraints_WithTheFrameworksParsers()
    {
        (string Parameter, string[] Good, string[] Bad)[] cases =
        [
            ("{v:int}", ["42", "-7", "2147483647"], ["2147483648", "4.2", "x"]),
            ("{v:long}", ["9223372036854775807", "-9223372036854775808"], ["9223372036854775808", "x"]),
            ("{v:bool}", ["true", "FALSE"], ["yes", "1"]),
            ("{v:guid}", ["3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e", "3f2a8c1e0b6d4f3a9c1e2b6d4f3a9c1e", "{3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e}"], ["3f2a8c1e", "x"]),
            ("{v:decimal}", ["4.2", "-1", "1,000.5"], ["1e3", "x"]),
            ("{v:double}", ["4.2", "1e3", "NaN"], ["x"]),
            ("{v:datetime}", ["2026-11-08", "2026-11-08T10:30", "01.02.2024", "10:30"], ["not a date", "7", "42", "12345", "abc 5", "2024-13-01"]),
            ("{v:alpha}", ["abc"], ["ab1"]),
            ("{v:nonfile}", ["readme"], ["readme.md"]),
            ("{v:file}", ["readme.md", "a.b."], ["readme", "readme."]),
            ("{v:length(3)}", ["abc"], ["ab"]),
            ("{v:length(2,3)}", ["ab", "abc"], ["a", "abcd"]),
            ("{v:minlength(2):maxlength(3)}", ["ab"], ["a", "abcd"]),
            ("{v:min(10)}", ["10"], ["9", "x"]),
            ("{v:max(10)}", ["10"], ["11"]),
            ("{v:range(1,3)}", ["2"], ["4"]),
            ("{v:regex(^\\d{{3}}$)}", ["123"], ["12", "1234"]),
            ("{v:custom}", ["anything"], []),
        ];
        foreach (var (parameter, good, bad) in cases)
        {
            var matcher = new BelemetryPageRouteMatcher(["/x/" + parameter]);
            foreach (var value in good) Assert.IsNotNull(matcher.Match("/x/" + value, "/"), $"{parameter} {value}");
            foreach (var value in bad) Assert.IsNull(matcher.Match("/x/" + value, "/"), $"{parameter} {value}");
        }
    }

    [TestMethod]
    public void ADateTimeRoute_DoesNotClaimANumber()
    {
        string[] routes = ["/x/{d:datetime}", "/x/{n:int}"];
        Assert.AreEqual("/x/{n:int}", Match(routes, "/x/42"));
        Assert.AreEqual("/x/{d:datetime}", Match(routes, "/x/2026-11-08"));
        Assert.AreEqual("/archive/{n}", Match(["/archive/{date:datetime}"], "/archive/2"));
    }

    [TestMethod]
    public void MatchesSegmentsOfSeveralParts_FromTheRight_AndSkipsMalformedTemplates()
    {
        string[] routes = ["/files/{name}.{ext}", "/broken/{id", "/files/{name}"];
        CollectionAssert.AreEqual(new[] { "/files/{name}.{ext}", "/files/{name}" }, new BelemetryPageRouteMatcher(routes).Templates.ToArray());
        Assert.AreEqual("/files/{name}.{ext}", Match(routes, "/files/report.pdf"));
        Assert.AreEqual("/files/{name}", Match(routes, "/files/report"));
        Assert.AreEqual("/broken/{n}", Match(routes, "/broken/1"));

        // archive.tar + gz, as ASP.NET Core splits it - not archive + tar.gz.
        Assert.AreEqual("/files/{name}.{ext:alpha}", Match(["/files/{name}.{ext:alpha}"], "/files/archive.tar.gz"));
        Assert.AreEqual("/f/{a}-{b:int}", Match(["/f/{a}-{b:int}"], "/f/x-y-2"));
        Assert.AreEqual("/f/x-2-y", Match(["/f/{a}-{b:int}"], "/f/x-2-y"));
        Assert.AreEqual("/p/a{x}", Match(["/p/a{x}"], "/p/ab"));
        Assert.AreEqual("/p/ba1", Match(["/p/a{x}"], "/p/ba1"));
    }

    [TestMethod]
    public void LetsAnOptionalLastPart_BeLeftOut_WithItsDot()
    {
        string[] routes = ["/files/{name}.{ext?}"];
        Assert.AreEqual("/files/{name}.{ext?}", Match(routes, "/files/report.pdf"));
        Assert.AreEqual("/files/{name}.{ext?}", Match(routes, "/files/report"));
        Assert.AreEqual("/files/report.", Match(routes, "/files/report."));
    }

    [TestMethod]
    public void MatchesBelowTheBasePath_AndWritesTheTemplateAfterIt()
    {
        string[] routes = ["/orders/{id:int}", "/"];
        Assert.AreEqual("/shop/orders/{id:int}", Match(routes, "/shop/orders/42", "/shop/"));
        Assert.AreEqual("/shop/", Match(routes, "/shop", "/shop/"));
        Assert.AreEqual("/other/orders/{n}", Match(routes, "/other/orders/42", "/shop/"), "Outside the base path, no route applies.");
        Assert.AreEqual("/shop/", BelemetryPageRouteMatcher.BasePath("https://app.test/shop/"));
        Assert.AreEqual("/shop/", BelemetryPageRouteMatcher.BasePath("https://app.test/shop/index.html"));
        Assert.AreEqual("/", BelemetryPageRouteMatcher.BasePath(null));
    }

    [TestMethod]
    public void ListsATemplateOnce_HoweverItIsWritten()
    {
        CollectionAssert.AreEqual(new[] { "/", "/a" }, new BelemetryPageRouteMatcher(["/a/", "a", "/A", "~/a", "", "/"]).Templates.ToArray());
    }
}
