using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The scanner every other answer about a service-worker file is built on. It is deliberately not
/// a parser, so what matters is that it never MIS-reads: a '//' inside a regular expression is not
/// a comment, a ';' inside a string does not end a statement, and a value it reads wrongly must
/// not swallow the assignments after it.
/// </summary>
[TestClass]
public class JavaScriptSourceTests
{
    // -- StripComments ---------------------------------------------------------

    [TestMethod]
    public void StripComments_RemovesLineAndBlockComments_KeepingOffsets()
    {
        const string code = "self.a = 1; // trailing\n/* block */self.b = 2;";

        var stripped = JavaScriptSource.StripComments(code);

        Assert.AreEqual(code.Length, stripped.Length, "offsets must survive, or reported positions drift");
        Assert.Contains("self.a = 1;", stripped);
        Assert.Contains("self.b = 2;", stripped);
        Assert.DoesNotContain("trailing", stripped);
        Assert.DoesNotContain("block", stripped);
    }

    [TestMethod]
    public void StripComments_KeepsTheNewlineEndingALineComment()
    {
        var stripped = JavaScriptSource.StripComments("// note\nself.isPassive = true;");

        // Swallowing the newline would fold the statement below into the comment.
        Assert.Contains("\nself.isPassive = true;", stripped);
    }

    [TestMethod]
    public void StripComments_DoesNotTreatSlashesInsideAStringAsAComment()
    {
        var stripped = JavaScriptSource.StripComments("self.assetsUrl = 'https://cdn.example.com/a.js'; self.isPassive = true;");

        Assert.Contains("https://cdn.example.com/a.js", stripped);
        Assert.Contains("self.isPassive = true;", stripped);
    }

    [TestMethod]
    public void StripComments_DoesNotTreatSlashesInsideARegexLiteralAsAComment()
    {
        var stripped = JavaScriptSource.StripComments(@"self.assetsInclude = [/https:\/\/cdn\.example\.com\//]; self.isPassive = true;");

        Assert.Contains(@"https:\/\/cdn\.example\.com\/", stripped);
        Assert.Contains("self.isPassive = true;", stripped);
    }

    [TestMethod]
    public void StripComments_TerminatesOnAnUnclosedBlockComment()
    {
        const string code = "self.a = 1;\n/* never closed";

        var stripped = JavaScriptSource.StripComments(code);

        Assert.AreEqual(code.Length, stripped.Length);
        Assert.Contains("self.a = 1;", stripped);
        Assert.DoesNotContain("never closed", stripped);
    }

    [TestMethod]
    public void StripComments_HandlesEmptyInput()
    {
        Assert.AreEqual(string.Empty, JavaScriptSource.StripComments(string.Empty));
    }

    // -- ReadAssignments -------------------------------------------------------

    private static IReadOnlyList<JsAssignment> Read(string code)
        => JavaScriptSource.ReadAssignments(JavaScriptSource.StripComments(code), "self");

    [TestMethod]
    public void ReadAssignments_ReadsNameValueAndOrder()
    {
        var assignments = Read("self.mode = 'FullOffline';\nself.isPassive = false;");

        Assert.HasCount(2, assignments);
        Assert.AreEqual("mode", assignments[0].Name);
        Assert.AreEqual("'FullOffline'", assignments[0].Value);
        Assert.AreEqual("isPassive", assignments[1].Name);
        Assert.AreEqual("false", assignments[1].Value);
        Assert.IsLessThan(assignments[1].Index, assignments[0].Index, "indices must follow source order");
    }

    [TestMethod]
    public void ReadAssignments_ReadsCompoundAssignmentOperators()
    {
        // The shipped worker defaults its own settings this way.
        var assignments = Read("self.errorTolerance ||= 'lax';");

        Assert.ContainsSingle(assignments);
        Assert.AreEqual("errorTolerance", assignments[0].Name);
        Assert.AreEqual("'lax'", assignments[0].Value);
    }

    [TestMethod]
    public void ReadAssignments_IgnoresComparisonsAndArrowFunctions()
    {
        var assignments = Read("if (self.mode === 'FullOffline') { } if (self.isPassive == true) { } const f = self.handler => 1;");

        Assert.IsEmpty(assignments, "a comparison is not a setting");
    }

    [TestMethod]
    public void ReadAssignments_IgnoresPropertiesOfSomethingElse()
    {
        var assignments = Read("myself.isPassive = true;\nwindow.self.isPassive = true;\nother.self.mode = 'x';");

        Assert.IsEmpty(assignments);
    }

    [TestMethod]
    public void ReadAssignments_EndsAStatementAtALineBreakWhenTheSemicolonIsMissing()
    {
        var assignments = Read("self.isPassive = false\nself.caseInsensitiveUrl = true\n");

        Assert.HasCount(2, assignments);
        Assert.AreEqual("false", assignments[0].Value, "without ASI the first value swallows the rest of the file");
        Assert.AreEqual("true", assignments[1].Value);
    }

    [TestMethod]
    public void ReadAssignments_KeepsAMultiLineValueTogether()
    {
        var assignments = Read("""
            self.externalAssets = [
                { "url": "/" },
                { "url": "_framework/blazor.web.js" }
            ];
            self.isPassive = true;
            """);

        Assert.HasCount(2, assignments);
        Assert.Contains("blazor.web.js", assignments[0].Value);
        Assert.AreEqual("true", assignments[1].Value);
    }

    [TestMethod]
    public void ReadAssignments_KeepsALineBreakInsideBracketsFromEndingTheStatement()
    {
        var assignments = Read("self.assetsInclude = [\n  /\\.js$/,\n  /\\.css$/\n]\nself.isPassive = true\n");

        Assert.HasCount(2, assignments);
        Assert.Contains(".css", assignments[0].Value);
    }

    [TestMethod]
    public void ReadAssignments_ContinuesAcrossAValueItMayHaveReadWrongly()
    {
        // Resuming after the NAME rather than after the value is what keeps one odd value from
        // hiding every setting below it - the one failure mode a configuration review cannot have.
        var assignments = Read("self.weird = someCall(function () { return ';' });\nself.isPassive = false;");

        Assert.Contains(a => a.Name == "isPassive", assignments, "a later assignment must never be swallowed");
    }

    [TestMethod]
    public void ReadAssignments_TreatsSemicolonsInsideStringsAsPartOfTheValue()
    {
        var assignments = Read("self.noPrerenderQuery = 'a;b';\nself.isPassive = true;");

        Assert.AreEqual("'a;b'", assignments[0].Value);
        Assert.HasCount(2, assignments);
    }

    [TestMethod]
    public void ReadAssignments_DoesNotReportTheImportScriptsCallAsAnAssignment()
    {
        var assignments = Read(ServiceWorkerFixtures.Import);

        Assert.DoesNotContain(a => a.Name == "importScripts", assignments,
            "importScripts is a call, not an assignment");
    }

    [TestMethod]
    public void ReadAssignments_RefusesChainedAssignmentsThatOverlapPastTheLimit()
    {
        Assert.ThrowsExactly<ScriptTooComplexException>(() => Read(ServiceWorkerFixtures.ChainedAssignments));
    }

    // -- ReadLiterals ----------------------------------------------------------

    [TestMethod]
    public void ReadLiterals_ReadsRegexAndStringEntriesApart()
    {
        var literals = JavaScriptSource.ReadLiterals(@"[/\.js$/i, 'weather.json', ""/api/""]");

        Assert.AreSequenceEqual(new[] { @"/\.js$/i", "'weather.json'", @"""/api/""" }, literals.ToArray());
    }

    [TestMethod]
    public void ReadLiterals_KeepsASlashInsideACharacterClass()
    {
        var literals = JavaScriptSource.ReadLiterals(@"[/[a-z/]+\.js$/]");

        Assert.ContainsSingle(literals, "a '/' inside a character class does not end the pattern");
        Assert.AreEqual(@"/[a-z/]+\.js$/", literals[0]);
    }

    [TestMethod]
    public void ReadLiterals_KeepsRegexFlags()
    {
        var literals = JavaScriptSource.ReadLiterals("[/abc/gim]");

        Assert.AreEqual("/abc/gim", literals[0]);
    }

    [TestMethod]
    public void ReadLiterals_ReturnsNothingForAnExpressionBuiltFromVariables()
    {
        var literals = JavaScriptSource.ReadLiterals("[...basePatterns, ...extraPatterns]");

        Assert.IsEmpty(literals);
    }

    // -- ReadObjectLiteral / ReadObjectEntries ---------------------------------

    [TestMethod]
    public void ReadObjectLiteral_ReadsTheBodyOfANamedDeclaration()
    {
        var body = JavaScriptSource.ReadObjectLiteral("const defaultoptions = { sw: 'service-worker.js', log: 'warn' };", "const defaultoptions =");

        Assert.IsNotNull(body);
        Assert.Contains("sw: 'service-worker.js'", body);
    }

    [TestMethod]
    public void ReadObjectLiteral_BalancesNestedBracesAndIgnoresBracesInsideStrings()
    {
        var body = JavaScriptSource.ReadObjectLiteral("const o = { a: { b: 1 }, c: '}' , d: 2 }; const after = 9;", "const o =");

        Assert.IsNotNull(body);
        Assert.Contains("d: 2", body);
        Assert.DoesNotContain("const after", body, "the literal must end at its own closing brace");
    }

    [TestMethod]
    public void ReadObjectLiteral_ReturnsNullWhenTheDeclarationIsAbsent()
    {
        Assert.IsNull(JavaScriptSource.ReadObjectLiteral("const other = {};", "const missing ="));
    }

    // -- ReadArrayLiteral ------------------------------------------------------

    [TestMethod]
    public void ReadArrayLiteral_ReadsTheBodyOfANamedDeclaration()
    {
        var body = JavaScriptSource.ReadArrayLiteral(@"const DEFAULT = [/\.dll$/, /\.wasm$/]; const after = 9;", "const DEFAULT =");

        Assert.IsNotNull(body);
        Assert.AreSequenceEqual(new[] { @"/\.dll$/", @"/\.wasm$/" }, JavaScriptSource.ReadLiterals(body).ToArray());
    }

    [TestMethod]
    public void ReadArrayLiteral_IsNotEndedByABracketInsideARegexLiteral()
    {
        // A character class and an escaped bracket are pattern text, not the end of the list: a
        // scan that counted them would drop every entry written after the pattern.
        var body = JavaScriptSource.ReadArrayLiteral(@"const DEFAULT = [/^\[dev\]/, /[a-z]\]$/, /\.css$/];", "const DEFAULT =");

        Assert.IsNotNull(body);
        Assert.AreSequenceEqual(new[] { @"/^\[dev\]/", @"/[a-z]\]$/", @"/\.css$/" }, JavaScriptSource.ReadLiterals(body).ToArray());
    }

    [TestMethod]
    public void ReadArrayLiteral_IsNotEndedByABracketInsideAStringEntry()
    {
        var body = JavaScriptSource.ReadArrayLiteral(@"const DEFAULT = [']', 'app.css']; const after = 9;", "const DEFAULT =");

        Assert.IsNotNull(body);
        Assert.AreSequenceEqual(new[] { "']'", "'app.css'" }, JavaScriptSource.ReadLiterals(body).ToArray());
    }

    [TestMethod]
    public void ReadArrayLiteral_ReturnsNullWhenTheDeclarationIsAbsent()
    {
        Assert.IsNull(JavaScriptSource.ReadArrayLiteral("const other = [];", "const missing ="));
    }

    [TestMethod]
    public void ReadObjectEntries_ReadsKeysAndValuesInOrder()
    {
        var entries = JavaScriptSource.ReadObjectEntries(" sw: 'service-worker.js', stallTimeout: 60, persistStorage: false ");

        Assert.AreSequenceEqual(new[] { "sw", "stallTimeout", "persistStorage" }, entries.Select(e => e.Key).ToArray());
        Assert.AreEqual("'service-worker.js'", entries[0].Value);
        Assert.AreEqual("60", entries[1].Value);
        Assert.AreEqual("false", entries[2].Value);
    }

    [TestMethod]
    public void ReadObjectEntries_KeepsNestedStructuresWithTheirKey()
    {
        var entries = JavaScriptSource.ReadObjectEntries("a: { x: 1, y: 2 }, b: [1, 2, 3], c: 4");

        Assert.HasCount(3, entries);
        Assert.AreEqual("{ x: 1, y: 2 }", entries[0].Value);
        Assert.AreEqual("[1, 2, 3]", entries[1].Value);
        Assert.AreEqual("4", entries[2].Value);
    }

    // The failure this guards against is a scan that never advances, so it has to time out rather
    // than hang the whole run.
    [TestMethod, Timeout(5000, CooperativeCancellation = true)]
    public void ReadObjectEntries_SkipsWhatIsNotAPlainKeyWithoutSpinning()
    {
        // A spread and a quoted key are not `key:` pairs; the scan must step over them and finish.
        var entries = JavaScriptSource.ReadObjectEntries("...rest, 'quoted': 1, plain: 2");

        Assert.Contains(e => e.Key == "plain", entries, "the readable entry after unreadable ones must still be found");
    }

    [TestMethod]
    public void ReadObjectEntries_HandlesAnEmptyBody()
    {
        Assert.IsEmpty(JavaScriptSource.ReadObjectEntries("   "));
    }
}
