namespace Bit.Borm.Tests;

/// <summary>A clock that always says the same thing, so <c>today()</c> and <c>age()</c> can be asserted.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public sealed class ExpressionLine
{
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public List<int> Lots { get; set; } = [];
}

public sealed class ExpressionInvoice
{
    public string Customer { get; set; } = "";
    public List<ExpressionLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    public DateOnly? Issued { get; set; }
    public DateOnly? Due { get; set; }
}

/// <summary>
/// The language itself: what parses, what it means, and what it refuses. Each test reads as a sentence
/// about the behaviour a schema author relies on, because an expression is something people write by
/// hand into a JSON document with no compiler to catch them.
/// </summary>
[TestClass]
public class ExpressionLanguageTests
{
    private static object? Eval(string expression, IDictionary<string, object?>? data = null)
        => BormExpression.Parse(expression).Evaluate(new BormDictionaryDataAccessor(data ?? new Dictionary<string, object?>()));

    private static bool IsTrue(string expression, IDictionary<string, object?>? data = null)
        => BormExpression.Parse(expression).IsTrue(new BormDictionaryDataAccessor(data ?? new Dictionary<string, object?>()));

    [TestMethod]
    public void Arithmetic_follows_the_usual_precedence_and_parentheses_override_it()
    {
        Assert.AreEqual(7m, Eval("1 + 2 * 3"));
        Assert.AreEqual(9m, Eval("(1 + 2) * 3"));
        Assert.AreEqual(-4m, Eval("-(2 + 2)"));
        Assert.AreEqual(1m, Eval("10 % 3"));
    }

    [TestMethod]
    public void Money_adds_up_exactly_because_numbers_are_decimals()
    {
        // The classic binary-floating-point surprise, which a line total in money must never show.
        Assert.AreEqual(0.3m, Eval("0.1 + 0.2"));
        Assert.AreEqual(29.97m, Eval("{Qty} * {Price}", new Dictionary<string, object?> { ["Qty"] = 3L, ["Price"] = 9.99 }));
    }

    [TestMethod]
    public void A_division_by_zero_has_no_answer_rather_than_an_exception()
    {
        Assert.IsNull(Eval("1 / 0"));
        Assert.IsNull(Eval("5 % 0"));
    }

    [TestMethod]
    public void Arithmetic_with_a_missing_value_is_missing_and_text_joins_with_nothing_in_its_place()
    {
        var data = new Dictionary<string, object?> { ["First"] = "Ada" };
        Assert.IsNull(Eval("{Missing} * 2", data));
        Assert.AreEqual("Ada ", Eval("{First} + ' ' + {Last}", data));
    }

    [TestMethod]
    public void A_comparison_with_a_missing_value_is_unknown_so_a_condition_is_false_and_its_negation_too()
    {
        // Unknown is not false: "not unknown" is still unknown, so neither branch claims the field.
        Assert.IsFalse(IsTrue("{Age} >= 18"));
        Assert.IsFalse(IsTrue("not ({Age} >= 18)"));
    }

    [TestMethod]
    public void A_missing_answer_read_directly_is_no_so_an_unticked_box_is_not_ticked()
    {
        // The same as BormConditionOperator.IsFalse: a dictionary-backed checkbox nobody touched has
        // no entry at all, and "not subscribed" must hold for it.
        Assert.IsTrue(IsTrue("not {Subscribe}"));
        Assert.IsFalse(IsTrue("{Subscribe}"));
    }

    [TestMethod]
    public void Three_valued_logic_lets_a_known_answer_decide_past_an_unknown_one()
    {
        Assert.IsFalse(IsTrue("{Age} > 1 and false"));
        Assert.IsTrue(IsTrue("{Age} > 1 or true"));
        Assert.IsFalse(IsTrue("{Age} > 1 or false"));
    }

    [TestMethod]
    public void Equality_treats_a_missing_value_as_a_value_of_its_own()
    {
        Assert.IsTrue(IsTrue("{Notes} = null"));
        Assert.IsTrue(IsTrue("{Kind} != 'business'"));
        Assert.IsTrue(IsTrue("{Kind} <> 'business'"));
    }

    [TestMethod]
    public void Equality_is_the_same_loose_equality_operator_conditions_use()
    {
        var data = new Dictionary<string, object?> { ["Count"] = 2L, ["Code"] = "ABC" };
        Assert.IsTrue(IsTrue("{Count} = 2.0", data));
        Assert.IsTrue(IsTrue("{Code} == 'abc'", data));
        // Two pieces of text compare as text, so a zip code keeps its leading zero.
        Assert.IsFalse(IsTrue("'01234' = '1234'"));
    }

    [TestMethod]
    public void Dates_compare_as_points_in_time_whatever_type_each_one_arrived_as()
    {
        var data = new Dictionary<string, object?> { ["Start"] = new DateOnly(2024, 5, 1), ["End"] = new DateTime(2024, 5, 3) };
        Assert.IsTrue(IsTrue("{End} > {Start}", data));
        Assert.IsTrue(IsTrue("{Start} = '2024-05-01'", data));
        Assert.IsTrue(IsTrue("{Start} < '2024-05-02'", data));
    }

    [TestMethod]
    public void Subtracting_dates_gives_the_days_between_them_and_adding_days_moves_one()
    {
        var data = new Dictionary<string, object?> { ["In"] = new DateOnly(2024, 5, 1), ["Out"] = new DateOnly(2024, 5, 4) };
        Assert.AreEqual(3m, Eval("{Out} - {In}", data));
        Assert.AreEqual(new DateOnly(2024, 5, 8), Eval("{Out} + 4", data));
    }

    [TestMethod]
    public void In_and_not_in_test_membership_of_a_list_literal_or_a_list_field()
    {
        var data = new Dictionary<string, object?> { ["Country"] = "fr", ["Allowed"] = new List<object?> { "de", "fr" } };
        Assert.IsTrue(IsTrue("{Country} in ['FR', 'DE']", data));
        Assert.IsTrue(IsTrue("{Country} in {Allowed}", data));
        Assert.IsTrue(IsTrue("{Country} not in ['us', 'ca']", data));
    }

    [TestMethod]
    public void Keywords_and_their_symbol_spellings_mean_the_same_thing_in_any_case()
    {
        Assert.IsTrue(IsTrue("TRUE AND NOT false"));
        Assert.IsTrue(IsTrue("true && !false"));
        Assert.IsTrue(IsTrue("false || true"));
    }

    [TestMethod]
    public void Text_takes_either_quote_and_a_doubled_quote_or_a_backslash_escapes_it()
    {
        Assert.AreEqual("it's", Eval("'it''s'"));
        Assert.AreEqual("say \"hi\"", Eval("\"say \\\"hi\\\"\""));
        Assert.AreEqual("a\nb", Eval("'a\\nb'"));
        // A backslash that escapes nothing is kept, so a pattern reads as it would anywhere else.
        Assert.AreEqual("^\\d{5}$", Eval("'^\\d{5}$'"));
        Assert.AreEqual("a\\b", Eval("'a\\\\b'"));
    }

    [TestMethod]
    public void A_comparison_cannot_be_chained()
    {
        var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse("1 < {x} < 5"));
        StringAssert.Contains(ex.Message, "and");
    }

    [TestMethod]
    public void A_bare_name_is_refused_with_a_hint_to_write_it_in_braces()
    {
        var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse("Age >= 18"));
        StringAssert.Contains(ex.Message, "{Age}");
        Assert.AreEqual(0, ex.Position);
    }

    [TestMethod]
    [DataRow("'unterminated", "closing quote")]
    [DataRow("{Age >= 18", "closing '}'")]
    [DataRow("{} = 1", "names no field")]
    [DataRow("{Lines[x].Total} = 1", "not a field path")]
    [DataRow("{a..b} = 1", "not a field path")]
    [DataRow("nosuch(1)", "no function")]
    [DataRow("round()", "takes 1 to 2 arguments")]
    [DataRow("iif(true, 1)", "takes 3 arguments")]
    [DataRow("(1 + 2", "Missing ')'")]
    [DataRow("1 +", "ends where a value")]
    [DataRow("1 2", "Unexpected")]
    [DataRow("", "cannot be empty")]
    [DataRow("matches({Code}, {Pattern})", "quoted literal")]
    [DataRow("matches({Code}, '(')", "not a valid regular expression")]
    [DataRow("dateDiff({a}, {b}, 'fortnights')", "not a unit")]
    [DataRow("1e999", "not a number")]
    public void Malformed_expressions_are_refused_with_a_message_saying_what_is_wrong(string text, string expected)
    {
        var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse(text));
        StringAssert.Contains(ex.Message, expected);
    }

    [TestMethod]
    public void TryParse_reports_the_problem_instead_of_throwing()
    {
        Assert.IsFalse(BormExpression.TryParse("{a} +", out var expression, out var error));
        Assert.IsNull(expression);
        Assert.IsNotNull(error);

        Assert.IsTrue(BormExpression.TryParse("{a} + 1", out expression, out error));
        Assert.IsNotNull(expression);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void The_dependencies_are_the_fields_named_and_a_reference_across_rows_depends_on_the_list()
    {
        var expression = BormExpression.Parse("sum({Lines[].Total}) + {Shipping} - {shipping} * {Lines[0].Total}");
        CollectionAssert.AreEquivalent(new[] { "Lines", "Shipping", "Lines[0].Total" }, expression.Dependencies.ToArray());
    }
}

[TestClass]
public class ExpressionFunctionTests
{
    private static object? Eval(string expression, IDictionary<string, object?>? data = null)
        => BormExpression.Parse(expression).Evaluate(new BormDictionaryDataAccessor(data ?? new Dictionary<string, object?>()));

    [TestMethod]
    public void Iif_evaluates_only_the_branch_it_takes()
    {
        var calls = 0;
        BormExpressionFunctions.Register("countedCall", _ => ++calls);
        try
        {
            Assert.AreEqual("yes", Eval("iif(1 < 2, 'yes', countedCall())"));
            Assert.AreEqual(0, calls);
            Assert.AreEqual(1, Eval("iif({Unknown} > 1, 'yes', countedCall())"));
        }
        finally
        {
            BormExpressionFunctions.Unregister("countedCall");
        }
    }

    [TestMethod]
    public void Coalesce_takes_the_first_value_that_has_been_answered()
    {
        var data = new Dictionary<string, object?> { ["Nick"] = "  ", ["Name"] = "Ada" };
        Assert.AreEqual("Ada", Eval("coalesce({Nick}, {Name}, 'anonymous')", data));
        Assert.AreEqual("anonymous", Eval("coalesce({None}, 'anonymous')"));
    }

    [TestMethod]
    public void Text_functions_work_invariantly_and_leave_a_missing_value_missing()
    {
        var data = new Dictionary<string, object?> { ["Code"] = "  ab-12  " };
        Assert.AreEqual("AB-12", Eval("upper(trim({Code}))", data));
        Assert.AreEqual("ab", Eval("substring(trim({Code}), 0, 2)", data));
        Assert.AreEqual("12", Eval("substring(trim({Code}), 3)", data));
        Assert.AreEqual(9, Eval("len({Code})", data));
        Assert.AreEqual(0, Eval("len({None})"));
        Assert.IsNull(Eval("lower({None})"));
        Assert.AreEqual("a1true", Eval("concat('a', 1, true)"));
        Assert.AreEqual(true, Eval("startsWith({Code}, '  AB')", data));
        Assert.AreEqual(true, Eval("contains({Code}, 'B-1')", data));
    }

    [TestMethod]
    public void Matches_checks_a_literal_pattern_with_a_time_limit()
    {
        var data = new Dictionary<string, object?> { ["Zip"] = "12345", ["Evil"] = new string('a', 40) + "!" };
        Assert.AreEqual(true, Eval("matches({Zip}, '^\\d{5}$')", data));
        Assert.AreEqual(false, Eval("matches({None}, '.*')"));
        // A pattern written to backtrack is either defused by the regex engine or abandoned at its
        // time limit — answered false or unknown, but answered, never a hang.
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var answer = Eval("matches({Evil}, '^(a|aa)+$')", data);
        Assert.IsTrue(answer is null or false, $"got {answer}");
        Assert.IsLessThan(5_000, watch.ElapsedMilliseconds);
    }

    [TestMethod]
    public void Rounding_takes_halves_away_from_zero_as_money_does()
    {
        Assert.AreEqual(3m, Eval("round(2.5)"));
        Assert.AreEqual(-3m, Eval("round(-2.5)"));
        Assert.AreEqual(1.24m, Eval("round(1.235, 2)"));
        Assert.AreEqual(2m, Eval("floor(2.9)"));
        Assert.AreEqual(3m, Eval("ceil(2.1)"));
        Assert.AreEqual(4m, Eval("abs(-4)"));
    }

    [TestMethod]
    public void Aggregates_take_values_and_lists_and_skip_what_has_not_been_answered()
    {
        var data = new Dictionary<string, object?> { ["A"] = 2L, ["B"] = null, ["List"] = new List<object?> { 1L, 5.5, null } };
        Assert.AreEqual(8.5m, Eval("sum({A}, {B}, {List})", data));
        Assert.AreEqual(0m, Eval("sum({Nothing})"));
        Assert.AreEqual(1m, Eval("min({List}, {A})", data));
        Assert.AreEqual(5.5m, Eval("max({List}, {A})", data));
        Assert.AreEqual(3, Eval("count({List}, {A}, {B})", data));
        Assert.AreEqual(8.5m / 3, Eval("avg({List}, {A})", data));
        Assert.IsNull(Eval("avg({Nothing})"));
    }

    [TestMethod]
    public void Date_functions_read_the_replaceable_clock()
    {
        var previous = BormExpressionFunctions.TimeProvider;
        BormExpressionFunctions.TimeProvider = new FixedClock(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        try
        {
            var data = new Dictionary<string, object?>
            {
                ["Born"] = new DateOnly(2008, 3, 16), ["Joined"] = "2025-01-31", ["Left"] = new DateTime(2025, 3, 30)
            };
            // One day short of an eighteenth birthday is seventeen.
            Assert.AreEqual(17, Eval("age({Born})", data));
            Assert.AreEqual(new DateTime(2026, 3, 15), Eval("today()"));
            Assert.AreEqual(2008, Eval("year({Born})", data));
            Assert.AreEqual(true, Eval("{Born} > addYears(today(), -18)", data));
            // 31 January to 30 March is one whole month: the second has not come round by its day.
            Assert.AreEqual(1L, Eval("dateDiff({Joined}, {Left}, 'months')", data));
            Assert.AreEqual(58L, Eval("dateDiff({Joined}, {Left})", data));
            Assert.AreEqual(new DateOnly(2008, 4, 16), Eval("addMonths({Born}, 1)", data));
            Assert.AreEqual(new DateOnly(2008, 3, 26), Eval("addDays({Born}, 10)", data));
        }
        finally
        {
            BormExpressionFunctions.TimeProvider = previous;
        }
    }

    [TestMethod]
    public void A_date_moved_beyond_the_calendar_has_no_answer()
    {
        var data = new Dictionary<string, object?> { ["D"] = new DateOnly(2024, 1, 1) };
        Assert.IsNull(Eval("addDays({D}, 99999999999)", data));
        Assert.IsNull(Eval("addYears({D}, 99999)", data));
        Assert.IsNull(Eval("{D} + 1e20", data));
    }

    [TestMethod]
    public void An_application_registers_its_own_function_but_cannot_replace_a_built_in_one()
    {
        BormExpressionFunctions.Register("vatRate", args => args[0] is "DE" ? 0.19m : 0.2m, minArgs: 1, maxArgs: 1);
        try
        {
            Assert.AreEqual(19m, Eval("100 * vatRate('DE')"));
            Assert.IsTrue(BormExpressionFunctions.Names.Contains("vatRate"));
            Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse("vatRate()"));
        }
        finally
        {
            Assert.IsTrue(BormExpressionFunctions.Unregister("vatRate"));
        }

        Assert.ThrowsExactly<ArgumentException>(() => BormExpressionFunctions.Register("sum", _ => 0));
        Assert.ThrowsExactly<ArgumentException>(() => BormExpressionFunctions.Register("and", _ => 0));
        Assert.ThrowsExactly<ArgumentException>(() => BormExpressionFunctions.Register("tax-rate", _ => 0));
        Assert.IsFalse(BormExpressionFunctions.Unregister("sum"));
        Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse("vatRate('DE')"));
    }
}

/// <summary>
/// References that walk into the form: a row's own fields, every row of a list, a list inside a row —
/// on a typed model and on a dictionary alike.
/// </summary>
[TestClass]
public class ExpressionReferenceTests
{
    private static ExpressionInvoice Invoice() => new()
    {
        Customer = "Contoso",
        Lines =
        [
            new() { Product = "A", Quantity = 2, UnitPrice = 1.5m, LineTotal = 3m, Lots = [1, 2] },
            new() { Product = "B", Quantity = 1, UnitPrice = 10m, LineTotal = 10m, Lots = [3] }
        ]
    };

    [TestMethod]
    public void Every_row_reference_reads_one_value_per_row_and_feeds_an_aggregate()
    {
        var data = new BormModelDataAccessor(Invoice());
        Assert.AreEqual(13m, BormExpression.Parse("sum({Lines[].LineTotal})").Evaluate(data));
        Assert.AreEqual(2, BormExpression.Parse("count({Lines})").Evaluate(data));
        Assert.AreEqual(2m, BormExpression.Parse("max({Lines[].Quantity})").Evaluate(data));
    }

    [TestMethod]
    public void A_list_inside_each_row_flattens_into_one_list()
    {
        var data = new BormModelDataAccessor(Invoice());
        Assert.AreEqual(6m, BormExpression.Parse("sum({Lines[].Lots[]})").Evaluate(data));
    }

    [TestMethod]
    public void A_reference_inside_a_row_means_that_row_before_the_root()
    {
        var data = BormScopedDataReader.For(new BormModelDataAccessor(Invoice()), "Lines[1]");
        Assert.AreEqual(10m, BormExpression.Parse("{Quantity} * {UnitPrice}").Evaluate(data));
        // A name the row does not have still resolves from the root.
        Assert.AreEqual("Contoso", BormExpression.Parse("{Customer}").Evaluate(data));
        Assert.AreEqual(3m, BormExpression.Parse("sum({Lots[]})").Evaluate(data));
    }

    [TestMethod]
    public void Every_row_reference_works_over_a_dictionary_store_too()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["Shares"] = new List<object?>
            {
                new Dictionary<string, object?> { ["Percent"] = 60L },
                new Dictionary<string, object?> { ["Percent"] = 40.0 }
            }
        });
        Assert.IsTrue(BormExpression.Parse("sum({Shares[].Percent}) = 100").IsTrue(data));
    }
}

/// <summary>
/// Expressions an untrusted document can send. The parser is bounded in every direction, and neither
/// parsing nor evaluating may ever throw anything but the documented exception.
/// </summary>
[TestClass]
public class ExpressionSafetyTests
{
    [TestMethod]
    public void An_expression_longer_than_the_limit_is_refused_before_it_is_read()
    {
        var text = string.Join(" + ", Enumerable.Repeat("1", BormExpression.MaxLength));
        var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse(text));
        StringAssert.Contains(ex.Message, "at most");
    }

    [TestMethod]
    public void Deep_nesting_is_refused_rather_than_overflowing_the_stack()
    {
        foreach (var text in new[]
                 {
                     new string('(', 2000) + "1" + new string(')', 2000),
                     string.Concat(Enumerable.Repeat("not ", 1000)) + "true",
                     new string('-', 2000) + "1",
                     string.Concat(Enumerable.Repeat("abs(", 500)) + "1" + new string(')', 500),
                     new string('[', 1000) + new string(']', 1000)
                 })
        {
            var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse(text));
            StringAssert.Contains(ex.Message, "nest");
        }
    }

    [TestMethod]
    public void An_expression_with_too_many_parts_is_refused()
    {
        var text = string.Join("+", Enumerable.Repeat("1", 1500));
        var ex = Assert.ThrowsExactly<BormExpressionException>(() => BormExpression.Parse(text));
        StringAssert.Contains(ex.Message, "parts");
    }

    [TestMethod]
    public void The_largest_accepted_expression_still_evaluates()
    {
        var text = string.Join("+", Enumerable.Repeat("1", 500));
        Assert.AreEqual(500m, BormExpression.Parse(text).Evaluate(new BormDictionaryDataAccessor()));
    }

    [TestMethod]
    public void Random_input_never_breaks_the_parser_or_the_evaluator()
    {
        // A seeded fuzz over the language's own tokens and some that are not, so the sequences are
        // close enough to real expressions to reach deep into the parser.
        string[] tokens =
        [
            "{a}", "{b.c}", "{l[]}", "{l[0].x}", "{", "}", "[", "]", "(", ")", ",", "+", "-", "*", "/", "%",
            "=", "==", "!=", "<>", "<", "<=", ">", ">=", "and", "or", "not", "in", "!", "&&", "||",
            "true", "false", "null", "0", "1", "2.5", "1e3", "'x'", "\"y\"", "'", "\"", "\\",
            "sum(", "iif(", "len(", "matches(", "dateDiff(", "today()", "age(", "round(", "x", "é", " "
        ];
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["a"] = 3L, ["b"] = new Dictionary<string, object?> { ["c"] = "text" },
            ["l"] = new List<object?> { new Dictionary<string, object?> { ["x"] = 1.5 }, null }
        });

        var random = new Random(20261025);
        var parsed = 0;
        for (var i = 0; i < 20_000; i++)
        {
            var count = random.Next(1, 8);
            var text = string.Concat(Enumerable.Range(0, count).Select(_ => tokens[random.Next(tokens.Length)] + (random.Next(3) == 0 ? " " : "")));
            BormExpression expression;
            try
            {
                expression = BormExpression.Parse(text);
            }
            catch (BormExpressionException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Parsing '{text}' threw {ex.GetType().Name}: {ex.Message}");
                return;
            }

            parsed++;
            try
            {
                expression.Evaluate(data);
                expression.IsTrue(data);
                _ = expression.Dependencies;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Evaluating '{text}' threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        // The corpus has to reach past the parser's front door, or it proves nothing about evaluation.
        Assert.IsGreaterThan(500, parsed);
    }
}
