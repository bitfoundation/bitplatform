using System.Globalization;

namespace Bit.Borm.Tests;

[TestClass]
public class ValueConverterTests
{
    [TestMethod]
    public void Formats_dates_and_times_for_html_inputs()
    {
        Assert.AreEqual("2024-03-05", BormValueConverter.ToInputString(new DateTime(2024, 3, 5, 14, 30, 0), BormFieldType.Date));
        Assert.AreEqual("14:30", BormValueConverter.ToInputString(new DateTime(2024, 3, 5, 14, 30, 0), BormFieldType.Time));
        Assert.AreEqual("2024-03-05T14:30", BormValueConverter.ToInputString(new DateTime(2024, 3, 5, 14, 30, 0), BormFieldType.DateTime));
        Assert.AreEqual("2024-03-05", BormValueConverter.ToInputString(new DateOnly(2024, 3, 5), BormFieldType.Date));
        Assert.AreEqual("09:15", BormValueConverter.ToInputString(new TimeOnly(9, 15), BormFieldType.Time));
    }

    [TestMethod]
    public void Formats_timespan_as_html_time_not_dotnet_default()
    {
        // TimeSpan.ToString() would produce "02:30:00", which <input type="time"> rejects.
        Assert.AreEqual("02:30", BormValueConverter.ToInputString(TimeSpan.FromMinutes(150), BormFieldType.Time));
    }

    [TestMethod]
    public void Parses_timespan_from_html_time_value()
    {
        var parsed = BormValueConverter.FromInputString("02:30", typeof(TimeSpan), BormFieldType.Time);
        Assert.AreEqual(TimeSpan.FromMinutes(150), parsed);
    }

    [TestMethod]
    public void Numbers_round_trip_invariantly_under_a_comma_decimal_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.AreEqual("1.5", BormValueConverter.ToInputString(1.5d, BormFieldType.Number));
            Assert.AreEqual("1.5", BormValueConverter.ToInputString(1.5m, BormFieldType.Number));
            Assert.AreEqual(1.5m, BormValueConverter.FromInputString("1.5", typeof(decimal), BormFieldType.Number));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void Empty_input_clears_a_nullable_and_defaults_a_value_type()
    {
        Assert.IsNull(BormValueConverter.FromInputString("", typeof(int?), BormFieldType.Integer));
        Assert.AreEqual(0, BormValueConverter.FromInputString("", typeof(int), BormFieldType.Integer));
        Assert.IsNull(BormValueConverter.FromInputString("", typeof(string), BormFieldType.Text));
    }

    [TestMethod]
    public void Unparseable_input_is_returned_unchanged_so_the_user_can_fix_it()
    {
        Assert.AreEqual("abc", BormValueConverter.FromInputString("abc", typeof(int), BormFieldType.Integer));
    }

    [TestMethod]
    public void Coerces_a_string_list_into_a_typed_enum_list()
    {
        var ok = BormValueConverter.TryCoerce(
            new List<string> { "Business", "Personal" }, typeof(List<AccountType>), out var result);

        Assert.IsTrue(ok);
        var list = Assert.IsExactInstanceOfType<List<AccountType>>(result);
        Assert.AreSequenceEqual([AccountType.Business, AccountType.Personal], list);
    }

    [TestMethod]
    public void Coerces_a_string_list_into_an_array()
    {
        var ok = BormValueConverter.TryCoerce(new List<string> { "a", "b" }, typeof(string[]), out var result);
        Assert.IsTrue(ok);
        Assert.AreSequenceEqual(new[] { "a", "b" }, Assert.IsExactInstanceOfType<string[]>(result));
    }

    [TestMethod]
    public void Reports_failure_rather_than_throwing_on_an_impossible_conversion()
    {
        Assert.IsFalse(BormValueConverter.TryCoerce("not-a-number", typeof(int), out _));
    }

    [TestMethod]
    public void A_double_reads_as_a_decimal_at_fifteen_significant_digits()
    {
        // From .NET 11 the runtime's own conversion turns 0.3 into 0.2999999999999999888977697537.
        Assert.AreEqual(0.3m, BormNumber.FromDouble(0.3));
        Assert.AreEqual(0.3m, BormNumber.FromDouble(0.1 + 0.2));
        Assert.AreEqual(1234567.89m, BormNumber.FromDouble(1234567.89));
        Assert.AreEqual(-123.4567m, BormNumber.FromDouble(-123.4567));
        Assert.AreEqual(0m, BormNumber.FromDouble(1e-30));
        Assert.AreEqual(0.1m, BormNumber.FromSingle(0.1f));
    }

    [TestMethod]
    public void A_double_coerced_to_decimal_is_the_number_it_was_written_as()
    {
        Assert.IsTrue(BormValueConverter.TryCoerce(0.3d, typeof(decimal), out var fromDouble));
        Assert.AreEqual(0.3m, fromDouble);
        Assert.IsTrue(BormValueConverter.TryCoerce(0.1f, typeof(decimal?), out var fromFloat));
        Assert.AreEqual(0.1m, fromFloat);
        Assert.IsFalse(BormValueConverter.TryCoerce(double.NaN, typeof(decimal), out _));
        Assert.IsFalse(BormValueConverter.TryCoerce(1e30, typeof(decimal), out _));
    }

    [TestMethod]
    public void Invariant_string_is_stable_across_cultures()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Assert.AreEqual("1.25", BormValueConverter.ToInvariantString(1.25d));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}

[TestClass]
public class TypedAccessorTests
{
    [TestMethod]
    public void Creates_array_elements_of_the_declared_type()
    {
        var model = new RegistrationModel();
        var data = new BormModelDataAccessor(model);

        // Items is empty: writing through an index has to materialise a LineItem, not a bare object.
        data.SetValue("Items[0].Product", "Widget");

        Assert.ContainsSingle(model.Items);
        Assert.AreEqual("Widget", model.Items[0].Product);
        Assert.IsFalse(data.LastWriteFailed);
    }

    [TestMethod]
    public void An_unconvertible_write_is_reported_not_thrown()
    {
        var model = new TypedModel { Count = 7 };
        var data = new BormModelDataAccessor(model);

        data.SetValue("Count", "not-a-number");

        Assert.IsTrue(data.LastWriteFailed);
        Assert.AreEqual(7, model.Count); // last valid value preserved
    }

    [TestMethod]
    public void Writes_multi_select_strings_into_a_typed_enum_collection()
    {
        var model = new TypedModel();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Accounts", new List<string> { "Business" });

        Assert.AreSequenceEqual([AccountType.Business], model.Accounts);
    }

    [TestMethod]
    public void A_double_written_to_a_decimal_property_keeps_the_value_it_was_written_as()
    {
        var model = new TypedModel();
        var data = new BormModelDataAccessor(model);

        // A default of 9.99 in code or in a JSON schema arrives as a double.
        data.SetValue("Price", 9.99d);

        Assert.AreEqual(9.99m, model.Price);
        Assert.IsFalse(data.LastWriteFailed);
    }

    [TestMethod]
    public void Writes_scalars_of_every_supported_shape()
    {
        var model = new TypedModel();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Duration", "01:45");
        data.SetValue("Day", "2024-03-05");
        data.SetValue("Moment", "09:15");
        data.SetValue("Id", "8a1e2f3d-0000-4000-8000-000000000001");
        data.SetValue("Website", "https://example.com");
        data.SetValue("OptionalCount", "");

        Assert.AreEqual(new TimeSpan(1, 45, 0), model.Duration);
        Assert.AreEqual(new DateOnly(2024, 3, 5), model.Day);
        Assert.AreEqual(new TimeOnly(9, 15), model.Moment);
        Assert.AreEqual(Guid.Parse("8a1e2f3d-0000-4000-8000-000000000001"), model.Id);
        Assert.AreEqual(new Uri("https://example.com"), model.Website);
        Assert.IsNull(model.OptionalCount);
    }

    [TestMethod]
    public void Resolves_element_types_for_lists_and_arrays()
    {
        var data = new BormModelDataAccessor(new TypedModel());
        Assert.AreEqual(typeof(AccountType), data.GetElementType("Accounts"));
        Assert.AreEqual(typeof(string), data.GetElementType("Tags"));
    }
}

[TestClass]
public class PathTests
{
    [TestMethod]
    public void Rejects_a_non_numeric_index_with_a_useful_message()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() => BormPath.Parse("items[x]"));
        Assert.Contains("items[x]", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Rejects_an_unterminated_index()
        => Assert.ThrowsExactly<FormatException>(() => BormPath.Parse("items[0"));

    [TestMethod]
    public void Recognises_paths_inside_a_subtree()
    {
        Assert.IsTrue(BormPath.IsAtOrUnder("Items", "Items"));
        Assert.IsTrue(BormPath.IsAtOrUnder("Items[0].Product", "Items"));
        Assert.IsTrue(BormPath.IsAtOrUnder("Address.City", "Address"));
        Assert.IsFalse(BormPath.IsAtOrUnder("ItemsTotal", "Items"));
    }

    [TestMethod]
    public void Reindexes_array_element_paths()
    {
        Assert.AreEqual("Items[1].Product", BormPath.Reindex("Items[2].Product", "Items", 1));
        Assert.AreEqual(2, BormPath.IndexIn("Items[2].Product", "Items"));
        Assert.IsNull(BormPath.IndexIn("Other[2]", "Items"));
    }
}
