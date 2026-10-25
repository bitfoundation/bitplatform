using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A value written in a fixed shape: what the user typed put into it once they leave the box, the formatted
/// text or only its characters stored, and a value that does not fit reported with the shape to write it in.
/// </summary>
[TestClass]
public class MaskTests : ComponentTestBase
{
    [TestMethod]
    [DataRow("5551234567")]
    [DataRow("555-123-4567")]
    [DataRow("(555) 123-4567")]
    [DataRow(" 555.123.4567 ")]
    [DataRow("(555)1234567")]
    public void However_the_separators_were_typed_the_digits_land_in_the_same_shape(string typed)
    {
        var mask = new BormMask("(000) 000-0000");

        Assert.IsTrue(mask.TryFormat(typed, out var formatted));
        Assert.AreEqual("(555) 123-4567", formatted);
        Assert.IsTrue(mask.TryUnmask(typed, out var raw));
        Assert.AreEqual("5551234567", raw);
    }

    [TestMethod]
    [DataRow("555123456", DisplayName = "too few")]
    [DataRow("55512345678", DisplayName = "too many")]
    [DataRow("555123456x", DisplayName = "a letter in a digit's place")]
    [DataRow("", DisplayName = "nothing")]
    public void A_value_that_does_not_fill_the_shape_exactly_does_not_fit(string typed)
        => Assert.IsFalse(new BormMask("(000) 000-0000").TryFormat(typed, out _));

    [TestMethod]
    public void Letters_are_capitalised_where_the_mask_says_and_literals_can_be_letters_and_digits_too()
    {
        var iban = new BormMask("AA00 0000 0000 0000 0000 00");
        Assert.IsTrue(iban.TryFormat("de89370400440532013000", out var formatted));
        Assert.AreEqual("DE89 3704 0044 0532 0130 00", formatted);
        Assert.AreEqual("AA## #### #### #### #### ##", iban.Display);
        Assert.IsFalse(iban.OnlyDigits);

        // "+1 " is literal: typed or not, it is not one of the ten digits.
        var phone = new BormMask("+1 000 000 0000");
        Assert.IsTrue(phone.TryFormat("+1 555 123 4567", out var withCode));
        Assert.IsTrue(phone.TryFormat("555 123 4567", out var withoutCode));
        Assert.AreEqual(withCode, withoutCode);

        // An escaped slot character is a literal: \A here is the letter A, always written.
        var reference = new BormMask(@"\A-000");
        Assert.IsTrue(reference.TryFormat("123", out var escaped));
        Assert.AreEqual("A-123", escaped);
        Assert.IsTrue(new BormMask("a*").TryFormat("x9", out var asTyped));
        Assert.AreEqual("x9", asTyped);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("()-")]
    [DataRow(@"000\")]
    public void A_mask_with_nothing_to_fill_or_a_dangling_escape_is_refused(string pattern)
        => Assert.ThrowsExactly<ArgumentException>(() => new BormMask(pattern));

    private static (BormState State, Dictionary<string, object?> Data) Phone(bool storeRaw = false)
    {
        var form = BormBuilder.Create().Text("phone", f => f.Label("Phone").Mask("(000) 000-0000", storeRaw)).Build();
        var data = new Dictionary<string, object?>();
        return (new BormState(form, new BormDictionaryDataAccessor(data)), data);
    }

    [TestMethod]
    public async Task What_is_typed_is_formatted_when_the_user_leaves_the_box_or_submits()
    {
        var (state, data) = Phone();
        var field = state.Definition.Fields[0];
        Assert.AreEqual("numeric", field.InputMode);

        state.SetValue("phone", "555 123 4567");
        Assert.IsTrue(state.NormalizeField(field, "phone"));
        Assert.AreEqual("(555) 123-4567", data["phone"]);

        // A field the user never left is put into shape by the submit.
        state.SetValueQuietly("phone", "5559876543");
        Assert.IsTrue(await state.SubmitAsync(_ => Task.CompletedTask));
        Assert.AreEqual("(555) 987-6543", data["phone"]);
    }

    [TestMethod]
    public async Task A_value_that_does_not_fit_is_left_as_typed_and_reported_with_its_shape()
    {
        var (state, data) = Phone();
        state.SetValue("phone", "555 12");

        Assert.IsFalse(await state.SubmitAsync(_ => Task.CompletedTask));
        Assert.AreEqual("555 12", data["phone"]);
        Assert.AreEqual("Enter it in the form (###) ###-####.", state.MessagesFor("phone").Single().Message);
        Assert.AreEqual(BormMessageKeys.Mask, state.MessagesFor("phone").Single().Code);

        // Blank is no answer at all, not a value of the wrong shape.
        state.SetValue("phone", "   ");
        state.NormalizeField(state.Definition.Fields[0], "phone");
        Assert.IsNull(data["phone"]);
        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void Stored_raw_the_model_gets_the_digits_and_the_box_and_the_review_still_show_the_shape()
    {
        var form = BormBuilder.Create()
            .Text("phone", f => f.Label("Phone").Mask("(000) 000-0000", storeRaw: true))
            .Review("summary")
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        cut.Find("#borm_phone").Change("555-123-4567");
        cut.Find("#borm_phone").Blur();

        Assert.AreEqual("5551234567", data["phone"]);
        Assert.AreEqual("(555) 123-4567", cut.Find("#borm_phone").GetAttribute("value"));
        StringAssert.Contains(cut.Find(".bit-borm-review").TextContent, "(555) 123-4567");
    }

    [TestMethod]
    public void The_mask_travels_through_json_schema_and_a_broken_one_fails_the_import_by_name()
    {
        var (state, _) = Phone(storeRaw: true);
        var json = BormJsonSchemaExporter.Export(state.Definition);
        StringAssert.Contains(json, "\"x-mask\": \"(000) 000-0000\"");

        var back = BormJsonSchemaImporter.Import(json).Fields[0];
        Assert.AreEqual("(000) 000-0000", back.Mask);
        Assert.IsTrue(back.MaskStoresRaw);
        Assert.IsTrue(back.Validators.Any(v => v is BormMaskRule));

        Assert.IsFalse(BormJsonSchemaImporter.TryImport("""
            { "type": "object", "properties": { "code": { "type": "string", "x-mask": "--" } } }
            """, out _, out var error));
        StringAssert.Contains(error, "x-mask of 'code'");
    }

    [TestMethod]
    [DataRow("SN-SNOW")]
    [DataRow("SNSNOW")]
    [DataRow("SNOW")]
    [DataRow("snow")]
    public void A_value_that_starts_like_the_masks_letters_still_fills_its_slots(string typed)
    {
        var mask = new BormMask("SN-AAAA");

        Assert.IsTrue(mask.TryFormat(typed, out var formatted), typed);
        Assert.AreEqual("SN-SNOW", formatted);
        Assert.IsTrue(mask.TryUnmask(typed, out var raw));
        Assert.AreEqual("SNOW", raw);
        Assert.IsFalse(mask.TryFormat("SNO", out _));
        Assert.IsFalse(mask.TryFormat("SNSNOWS", out _));
    }

    [TestMethod]
    public async Task A_value_stored_raw_that_starts_like_the_masks_letters_is_not_called_a_misfit()
    {
        var form = BormBuilder.Create().Text("code", f => f.Mask("SN-AAAA", storeRaw: true)).Build();
        var data = new Dictionary<string, object?> { ["code"] = "SNOW" };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        Assert.IsTrue(await state.ValidateAsync());
    }
}
