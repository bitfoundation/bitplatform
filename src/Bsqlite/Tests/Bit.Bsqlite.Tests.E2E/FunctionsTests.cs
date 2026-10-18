using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>The <c>ef_*</c> decimal functions, the <c>EF_DECIMAL</c> collation and <c>regexp</c>.</summary>
[TestClass]
public sealed class FunctionsTests : BsqliteTest
{
    /// <summary>
    /// The S4 data set: values chosen so lexicographic TEXT order and numeric order disagree (<c>9</c> vs
    /// <c>10</c>, <c>2.5</c> vs <c>100</c>), with zero, negatives, sub-unit scales, and nulls.
    /// </summary>
    /// <remarks>
    /// Every <c>ef_*</c> result below is what Microsoft.Data.Sqlite returns for the same call - the oracle these
    /// functions exist to imitate - which is the canonical <c>0.0###########################</c> form and not a
    /// plain ToString. <c>ef_multiply('10','3')</c> is <c>'30.0'</c> there, so it is <c>'30.0'</c> here.
    /// </remarks>
    private static readonly (int Id, string Value, string? Optional, int Bucket, string Text)[] Rows =
    [
        (Id: 1, Value: "0", Optional: null, Bucket: 0, Text: "alpha"),
        (Id: 2, Value: "10", Optional: "10", Bucket: 0, Text: "Alpha"),
        (Id: 3, Value: "9", Optional: null, Bucket: 0, Text: "beta-1"),
        (Id: 4, Value: "-5", Optional: "-5", Bucket: 0, Text: "gamma 42"),
        (Id: 5, Value: "2.5", Optional: "2.5", Bucket: 1, Text: ""),
        (Id: 6, Value: "0.1", Optional: null, Bucket: 1, Text: "a.b.c"),
        (Id: 7, Value: "100", Optional: "100", Bucket: 1, Text: "123"),
        (Id: 8, Value: "-0.25", Optional: "-0.25", Bucket: 1, Text: "x[y]z"),
        (Id: 9, Value: "1234.5678", Optional: "1234.5678", Bucket: 1, Text: "  spaced  "),
        (Id: 10, Value: "3", Optional: "3", Bucket: 1, Text: "delta\ntail"),
    ];

    // ---------------------------------------------------------------------------------------------
    // scalar decimal functions
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("ef_add(value, '1.5')",
        new string?[] { "1.5", "11.5", "10.5", "-3.5", "4.0", "1.6", "101.5", "1.25", "1236.0678", "4.5" }, DisplayName = "add")]
    [DataRow("ef_add(value, ef_negate('1.5'))",
        new string?[] { "-1.5", "8.5", "7.5", "-6.5", "1.0", "-1.4", "98.5", "-1.75", "1233.0678", "1.5" }, DisplayName = "subtract")]
    [DataRow("ef_multiply(value, '3')",
        new string?[] { "0.0", "30.0", "27.0", "-15.0", "7.5", "0.3", "300.0", "-0.75", "3703.7034", "9.0" }, DisplayName = "multiply")]
    [DataRow("ef_divide(value, '4')",
        new string?[] { "0.0", "2.5", "2.25", "-1.25", "0.625", "0.025", "25.0", "-0.0625", "308.64195", "0.75" }, DisplayName = "divide")]
    [DataRow("ef_mod(value, '3')",
        new string?[] { "0.0", "1.0", "0.0", "-2.0", "2.5", "0.1", "1.0", "-0.25", "1.5678", "0.0" }, DisplayName = "modulo")]
    [DataRow("ef_negate(value)",
        new string?[] { "0.0", "-10.0", "-9.0", "5.0", "-2.5", "-0.1", "-100.0", "0.25", "-1234.5678", "-3.0" }, DisplayName = "negate")]
    [DataRow("ef_add(optional, '1')",
        new string?[] { null, "11.0", null, "-4.0", "3.5", null, "101.0", "0.75", "1235.5678", "4.0" }, DisplayName = "nullable add")]
    public async Task Scalar_decimal_function(string expr, string?[] expected)
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, $"SELECT {expr} FROM rows ORDER BY id");
        AssertJson.AreEqual(JsonSerializer.Serialize(expected), Column(result));
    }

    [TestMethod]
    public async Task Scalar_decimal_functions_1_over_3_is_twenty_eight_threes_not_an_IEEE_float()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, "SELECT ef_divide('1', '3')");
        AssertJson.AreEqual("""[["0.3333333333333333333333333333"]]""", result.Rows);
    }

    [TestMethod]
    public async Task Scalar_decimal_functions_a_zero_divisor_yields_null_rather_than_an_error()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, "SELECT ef_divide('10', '0'), ef_mod('10', '0')");
        AssertJson.AreEqual("[[null, null]]", result.Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // aggregates
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Aggregates_sum_average_max_min()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page,
            "SELECT ef_sum(value), ef_avg(value), ef_max(value), ef_min(value) FROM rows");

        AssertJson.AreEqual("""[["1353.9178", "135.39178", "1234.5678", "-5.0"]]""", result.Rows);
    }

    [TestMethod]
    public async Task Aggregates_nullable_aggregates_skip_nulls()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page,
            "SELECT ef_sum(optional), ef_avg(optional), ef_max(optional), ef_min(optional) FROM rows");

        AssertJson.AreEqual(
            """
            [[
              "1344.8178",
              "192.11682857142857142857142857",
              "1234.5678",
              "-5.0"
            ]]
            """, result.Rows);
    }

    [TestMethod]
    public async Task Aggregates_grouped_sum_and_average()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page,
            "SELECT bucket, ef_sum(value), ef_avg(value) FROM rows GROUP BY bucket ORDER BY bucket");

        AssertJson.AreEqual(
            """
            [
              [0, "14.0", "3.5"],
              [1, "1339.9178", "223.31963333333333333333333333"]
            ]
            """, result.Rows);
    }

    [TestMethod]
    public async Task Aggregates_an_empty_group_yields_null_not_zero()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, "SELECT ef_sum(value), ef_avg(value) FROM rows WHERE id = 0");
        AssertJson.AreEqual("[[null, null]]", result.Rows);
    }

    /// <summary>
    /// A scalar read abandons a grouped query after its first row, leaving the remaining groups part-way
    /// through. Aggregate state is held against the address <c>sqlite3_aggregate_context</c> returns, which
    /// SQLite frees with the statement and hands out again, so anything left behind would be read as a running
    /// total by whichever aggregate lands on that address next. These pin the behaviour that matters - a later
    /// aggregate starts from nothing - rather than the mechanism.
    /// </summary>
    [TestMethod]
    public async Task Aggregates_a_grouped_query_abandoned_after_one_row_does_not_poison_the_next()
    {
        await OpenSeededAsync();

        var abandoned = (await ExecuteAsync(Page,
            new WireRequest("SELECT ef_sum(value) FROM rows GROUP BY bucket ORDER BY bucket", ResultKind: "scalar")))[0];
        AssertJson.AreEqual("""[["14.0"]]""", abandoned.Rows);

        // The same totals as a first run would produce.
        var again = await QueryAsync(Page,
            "SELECT bucket, ef_sum(value) FROM rows GROUP BY bucket ORDER BY bucket");
        AssertJson.AreEqual("""[[0, "14.0"], [1, "1339.9178"]]""", again.Rows);

        var whole = await QueryAsync(Page, "SELECT ef_sum(value) FROM rows");
        AssertJson.AreEqual("""[["1353.9178"]]""", whole.Rows);
    }

    [TestMethod]
    public async Task Aggregates_a_failing_aggregate_query_does_not_poison_the_next()
    {
        await OpenSeededAsync();

        await ExecuteExpectingFailureAsync(Page,
            new WireRequest("SELECT ef_sum(value) FROM rows WHERE no_such_column = 1"));

        var whole = await QueryAsync(Page, "SELECT ef_sum(value) FROM rows");
        AssertJson.AreEqual("""[["1353.9178"]]""", whole.Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // ordering and comparison
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Ordering_ORDER_BY_COLLATE_EF_DECIMAL_is_numeric_not_lexicographic()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, "SELECT id FROM rows ORDER BY value COLLATE EF_DECIMAL, id");
        AssertJson.AreEqual("[4, 8, 1, 6, 5, 10, 3, 2, 7, 9]", Column(result));
    }

    [TestMethod]
    public async Task Ordering_ORDER_BY_DESC_is_the_reverse_numeric_order()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page,
            "SELECT id FROM rows ORDER BY value COLLATE EF_DECIMAL DESC, id");
        AssertJson.AreEqual("[9, 7, 2, 3, 10, 5, 6, 1, 8, 4]", Column(result));
    }

    [TestMethod]
    public async Task Ordering_nullable_ORDER_BY_puts_SQLite_nulls_first_then_numeric_order()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page,
            "SELECT id FROM rows ORDER BY optional COLLATE EF_DECIMAL, id");
        AssertJson.AreEqual("[1, 3, 6, 4, 8, 5, 10, 2, 7, 9]", Column(result));
    }

    [TestMethod]
    public async Task Ordering_without_the_collation_9_sorts_after_100()
    {
        await OpenSeededAsync();

        // The failure this prevents: a missing collation looks like a working ORDER BY until the data set
        // contains values whose TEXT order and numeric order disagree.
        var result = await QueryAsync(Page, "SELECT value FROM rows WHERE value IN ('9', '100') ORDER BY value");
        AssertJson.AreEqual("""["100", "9"]""", Column(result));
    }

    [TestMethod]
    [DataRow("ef_compare(value, '2.5') > 0", new[] { 2, 3, 7, 9, 10 }, DisplayName = "greater than")]
    [DataRow("ef_compare(value, '2.5') >= 0", new[] { 2, 3, 5, 7, 9, 10 }, DisplayName = "greater or equal")]
    [DataRow("ef_compare(value, '2.5') < 0", new[] { 1, 4, 6, 8 }, DisplayName = "less than")]
    [DataRow("ef_compare(value, '0') < 0", new[] { 4, 8 }, DisplayName = "less than zero")]
    [DataRow("ef_compare(value, '9') = 0", new[] { 3 }, DisplayName = "equals via compare")]
    [DataRow("ef_compare(value, '9') <> 0", new[] { 1, 2, 4, 5, 6, 7, 8, 9, 10 }, DisplayName = "not equals via compare")]
    [DataRow("optional IS NULL", new[] { 1, 3, 6 }, DisplayName = "null check")]
    [DataRow("ef_compare(ef_multiply(value, '2'), '10') > 0", new[] { 2, 3, 7, 9 }, DisplayName = "computed filter")]
    public async Task Ordering_filter(string where, int[] expected)
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, $"SELECT id FROM rows WHERE {where} ORDER BY id");
        AssertJson.AreEqual(JsonSerializer.Serialize(expected), Column(result));
    }

    [TestMethod]
    public async Task Ordering_TEXT_equality_is_exact_which_is_why_the_stored_form_has_to_be_canonical()
    {
        await OpenSeededAsync();

        // These rows were seeded as literal TEXT, so `value` holds '9' rather than the canonical '9.0'. SQLite's
        // `=` does not care that they are the same number - and EF compiles `== 9m` to exactly this comparison,
        // against a literal it renders as '9.0'. That is the whole reason decimals are written through
        // toSqlText: a row stored in any other form is invisible to the lookup that goes looking for it.
        AssertJson.AreEqual("[3]", Column(await QueryAsync(Page, "SELECT id FROM rows WHERE value = '9'")));
        AssertJson.AreEqual("[]", Column(await QueryAsync(Page, "SELECT id FROM rows WHERE value = '9.0'")));
        AssertJson.AreEqual("[3]", Column(await QueryAsync(Page, "SELECT id FROM rows WHERE ef_compare(value, '9.0') = 0")));
    }

    [TestMethod]
    public async Task Ordering_an_ef_result_is_in_the_canonical_form_EF_compares_against()
    {
        await OpenSeededAsync();

        // The failure this prevents: writing `10` where Microsoft.Data.Sqlite writes `10.0`, so
        // `WHERE price = '10.0'` - which is what EF generates for `== 10m` - matches nothing.
        var result = await QueryAsync(Page,
            "SELECT ef_multiply('5', '2') = '10.0', ef_add('9', '1') = '10.0', ef_negate('10') = '-10.0'");
        AssertJson.AreEqual("[[1, 1, 1]]", result.Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // regexp
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DataRow(@"^a", new[] { 1, 6 }, DisplayName = @"REGEXP ^a")]
    [DataRow(@"^A", new[] { 2 }, DisplayName = @"REGEXP ^A")]
    [DataRow(@"a", new[] { 1, 2, 3, 4, 6, 9, 10 }, DisplayName = @"REGEXP a")]
    [DataRow(@"[0-9]+", new[] { 3, 4, 7 }, DisplayName = @"REGEXP [0-9]+")]
    [DataRow(@"^[0-9]+$", new[] { 7 }, DisplayName = @"REGEXP ^[0-9]+$")]
    [DataRow(@"\d", new[] { 3, 4, 7 }, DisplayName = @"REGEXP \d")]
    [DataRow(@"a\.b", new[] { 6 }, DisplayName = @"REGEXP a\.b")]
    [DataRow(@"\[y\]", new[] { 8 }, DisplayName = @"REGEXP \[y\]")]
    [DataRow(@"^$", new[] { 5 }, DisplayName = @"REGEXP ^$")]
    [DataRow(@"^\s+", new[] { 9 }, DisplayName = @"REGEXP ^\s+")]
    [DataRow(@"tail$", new[] { 10 }, DisplayName = @"REGEXP tail$")]
    [DataRow(@"(alpha|beta)", new[] { 1, 3 }, DisplayName = @"REGEXP (alpha|beta)")]
    [DataRow(@"z{1}", new[] { 8 }, DisplayName = @"REGEXP z{1}")]
    [DataRow(@"^.$", new int[] { }, DisplayName = @"REGEXP ^.$")]
    [DataRow(@"a(?=l)", new[] { 1 }, DisplayName = @"REGEXP a(?=l)")]
    [DataRow(@"a(?!l)", new[] { 1, 2, 3, 4, 6, 9, 10 }, DisplayName = @"REGEXP a(?!l)")]
    [DataRow(@"(\w)\1", new[] { 4 }, DisplayName = @"REGEXP (\w)\1")]
    public async Task REGEXP(string pattern, int[] expected)
    {
        await OpenSeededAsync();

        var result = await QueryAsync(
            Page,
            $"SELECT id FROM rows WHERE text REGEXP {SqlQuote(pattern)} ORDER BY id");
        AssertJson.AreEqual(JsonSerializer.Serialize(expected), Column(result));
    }

    [TestMethod]
    public async Task Regexp_pattern_input_is_the_argument_order_SQLite_uses()
    {
        await OpenSeededAsync();

        // A transposition would pass '^a' against 'alpha' if someone called IsMatch-style regexp(input, pattern)
        // and the test only used the operator.
        var result = await QueryAsync(Page, "SELECT regexp('^a', 'alpha'), regexp('alpha', '^a')");
        AssertJson.AreEqual("[[1, 0]]", result.Rows);
    }

    [TestMethod]
    public async Task Regexp_a_null_operand_yields_null()
    {
        await OpenSeededAsync();

        var result = await QueryAsync(Page, "SELECT regexp(NULL, text), text REGEXP NULL FROM rows WHERE id = 1");
        AssertJson.AreEqual("[[null, null]]", result.Rows);
    }

    // The file's beforeEach: every test starts from the seeded data set.
    private async Task OpenSeededAsync()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = "functions.db" });
        await SeedAsync();
    }

    private async Task SeedAsync()
    {
        await ExecAsync(Page,
            """
            CREATE TABLE rows (
              id INTEGER PRIMARY KEY,
              value TEXT NOT NULL,
              optional TEXT,
              bucket INTEGER NOT NULL,
              text TEXT NOT NULL
            )
            """);

        foreach (var row in Rows)
        {
            await ExecAsync(
                Page,
                "INSERT INTO rows (id, value, optional, bucket, text) VALUES (@id, @value, @optional, @bucket, @text)",
                new WireParameter("@id", row.Id),
                new WireParameter("@value", row.Value),
                new WireParameter("@optional", row.Optional),
                new WireParameter("@bucket", row.Bucket),
                new WireParameter("@text", row.Text));
        }
    }

    /// <summary>The first cell of every row, as JSON for <see cref="AssertJson"/>.</summary>
    private static JsonElement Column(WireResult result)
        => JsonSerializer.SerializeToElement(result.Rows.EnumerateArray().Select(row => row[0]).ToArray());

    private static string SqlQuote(string value) => $"'{value.Replace("'", "''")}'";
}
