using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Form;

[TestClass]
public class BitAiFormTests : BitAiTestBase
{
    private static BitAiFormDefinition Definition => new()
    {
        Title = "Book a table",
        Fields =
        [
            new BitAiFormField("guests") { Type = BitAiFieldType.Number, Required = true, Minimum = 1, Maximum = 8 },
            new BitAiFormField("email") { Type = BitAiFieldType.Email },
            new BitAiFormField("notes") { Type = BitAiFieldType.MultilineText },
        ],
    };

    // Every field is a core input that renders a label bound to its control, so a control is found through its label.
    private static IElement InputOf(IRenderedComponent<BitAiForm> form, string label)
    {
        var labelElement = form.FindAll("label[for]").First(l => l.TextContent.Trim() == label);
        return form.Find($"[id='{labelElement.GetAttribute("for")}']");
    }

    private static IElement Submit(IRenderedComponent<BitAiForm> form) => form.Find("button.bit-aifrm-sbt");

    [TestMethod]
    public void BitAiFormShouldRenderTheRootClassTitleAndDescription()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, Definition with { Description = "For tonight." }));

        var root = form.Find(".bit-aifrm");

        Assert.AreEqual("Book a table", root.QuerySelector(".bit-aifrm-ttl")!.TextContent);
        Assert.AreEqual("For tonight.", root.QuerySelector(".bit-aifrm-des")!.TextContent);
        Assert.AreEqual(form.Instance.UniqueId, root.Id);
    }

    [TestMethod]
    public void EveryFieldGetsALabelBoundToItsControl()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, Definition));

        var label = form.Find("label[for]");
        var id = label.GetAttribute("for");

        Assert.IsFalse(string.IsNullOrEmpty(id));
        Assert.AreEqual("INPUT", form.Find($"[id='{id}']").TagName);
        StringAssert.Contains(form.Markup, "Guests");
        Assert.AreEqual("TEXTAREA", InputOf(form, "Notes").TagName);
        Assert.AreEqual("email", InputOf(form, "Email").GetAttribute("type"));
    }

    [TestMethod]
    public void SubmittingWithAMissingRequiredFieldReportsItAndDoesNotSubmit()
    {
        IReadOnlyDictionary<string, string?>? submitted = null;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.OnSubmit, values => submitted = values));

        Submit(form).Click();

        Assert.IsNull(submitted);
        StringAssert.Contains(form.Markup, "Guests is required.");
        Assert.AreEqual("true", InputOf(form, "Guests").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void AValidFormSubmitsItsValues()
    {
        IReadOnlyDictionary<string, string?>? submitted = null;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.OnSubmit, values => submitted = values));

        InputOf(form, "Guests").Change("4");
        Submit(form).Click();

        Assert.IsNotNull(submitted);
        Assert.AreEqual("4", submitted!["guests"]);
    }

    [TestMethod]
    public void NumericBoundsAreEnforced()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, Definition));

        // The number field keeps an out-of-range value (NoClamp) for the validation to report, rather than quietly
        // correcting it to the bound.
        InputOf(form, "Guests").Change("20");
        Submit(form).Click();

        StringAssert.Contains(form.Markup, "at most 8");
        Assert.AreEqual("20", form.Instance.Values["guests"]);
    }

    [TestMethod]
    public void TypingIntoAFieldClearsItsError()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, new BitAiFormDefinition
        {
            Fields = [new BitAiFormField("city") { Required = true }],
        }));

        Submit(form).Click();
        Assert.AreEqual("true", InputOf(form, "City").GetAttribute("aria-invalid"));

        InputOf(form, "City").Input("Oslo");

        Assert.IsFalse(form.Markup.Contains("City is required."));
        Assert.AreEqual("Oslo", form.Instance.Values["city"]);
    }

    [TestMethod]
    public void FieldsStreamingInDoNotClearWhatTheUserHasTyped()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, Definition));
        InputOf(form, "Guests").Change("3");

        form.Render(p => p.Add(c => c.Definition, Definition with
        {
            Fields = [.. Definition.Fields, new BitAiFormField("seating")],
        }));

        StringAssert.Contains(form.Markup, "Seating");
        Assert.AreEqual("3", form.Instance.Values["guests"]);
    }

    [TestMethod]
    public void AFormBuiltFromJsonSchemaPicksSensibleControls()
    {
        var schema = JsonDocument.Parse("""
            {
              "title": "Contact",
              "type": "object",
              "required": ["email"],
              "properties": {
                "email": { "type": "string", "format": "email" },
                "age": { "type": "integer", "minimum": 0 },
                "subscribed": { "type": "boolean" },
                "plan": { "type": "string", "enum": ["free", "pro"] }
              }
            }
            """).RootElement;

        var definition = BitAiFormDefinition.FromJsonSchema(schema);

        Assert.AreEqual("Contact", definition.Title);
        Assert.AreEqual(BitAiFieldType.Email, definition.Fields.Single(f => f.Name == "email").Type);
        Assert.AreEqual(BitAiFieldType.Number, definition.Fields.Single(f => f.Name == "age").Type);
        Assert.AreEqual(BitAiFieldType.Boolean, definition.Fields.Single(f => f.Name == "subscribed").Type);
        Assert.AreEqual(BitAiFieldType.Select, definition.Fields.Single(f => f.Name == "plan").Type);
        Assert.IsTrue(definition.Fields.Single(f => f.Name == "email").Required);
    }

    [TestMethod]
    public void ANullableTypeListAndJunkKeywordsDoNotThrow()
    {
        var schema = JsonDocument.Parse("""
            {
              "title": 5,
              "type": "object",
              "required": [1, "nickname", null],
              "properties": {
                "nickname": { "type": ["string", "null"], "title": 7, "description": ["x"] },
                "age": { "type": ["null", "integer"] },
                "odd": { "type": 3 }
              }
            }
            """).RootElement;

        var definition = BitAiFormDefinition.FromJsonSchema(schema);

        Assert.IsNull(definition.Title);
        var nickname = definition.Fields.Single(f => f.Name == "nickname");
        Assert.AreEqual(BitAiFieldType.Text, nickname.Type);
        Assert.IsFalse(nickname.Required);
        Assert.AreEqual("Nickname", nickname.DisplayLabel);
        var age = definition.Fields.Single(f => f.Name == "age");
        Assert.AreEqual(BitAiFieldType.Number, age.Type);
        Assert.IsTrue(age.Integer);
        Assert.AreEqual(BitAiFieldType.Text, definition.Fields.Single(f => f.Name == "odd").Type);
    }

    [TestMethod]
    public void SchemaKeywordsMapOntoFieldConstraints()
    {
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "qty": { "type": "integer", "exclusiveMinimum": 0, "maximum": 10, "multipleOf": 2, "default": 4 },
                "legacy": { "type": "number", "minimum": 1, "exclusiveMinimum": true },
                "code": { "type": "string", "minLength": 2, "maxLength": 4, "readOnly": true },
                "kind": { "const": "booking" },
                "plan": { "type": "string", "enum": ["free", "pro"], "enumNames": ["Free", "Professional"] },
                "size": { "oneOf": [ { "const": "s", "title": "Small" }, { "const": "l", "title": "Large" } ] },
                "when": { "type": "string", "format": "time" },
                "site": { "type": "string", "format": "uri" },
                "at": { "anyOf": [ { "type": "string", "format": "date-time" }, { "type": "null" } ] }
              },
              "required": ["at"]
            }
            """).RootElement;

        var fields = BitAiFormDefinition.FromJsonSchema(schema).Fields.ToDictionary(f => f.Name);

        Assert.AreEqual(0, fields["qty"].ExclusiveMinimum);
        Assert.AreEqual(10, fields["qty"].Maximum);
        Assert.AreEqual(2, fields["qty"].MultipleOf);
        Assert.AreEqual("4", fields["qty"].Value);
        Assert.AreEqual(1, fields["legacy"].ExclusiveMinimum);
        Assert.IsNull(fields["legacy"].Minimum);
        Assert.AreEqual(2, fields["code"].MinLength);
        Assert.AreEqual(4, fields["code"].MaxLength);
        Assert.IsTrue(fields["code"].ReadOnly);
        Assert.AreEqual("booking", fields["kind"].Value);
        Assert.IsTrue(fields["kind"].ReadOnly);
        CollectionAssert.AreEqual(new[] { "Free", "Professional" }, fields["plan"].OptionLabels!.ToArray());
        Assert.AreEqual(BitAiFieldType.Select, fields["size"].Type);
        CollectionAssert.AreEqual(new[] { "s", "l" }, fields["size"].Options!.ToArray());
        CollectionAssert.AreEqual(new[] { "Small", "Large" }, fields["size"].OptionLabels!.ToArray());
        Assert.AreEqual(BitAiFieldType.Time, fields["when"].Type);
        Assert.AreEqual(BitAiFieldType.Url, fields["site"].Type);
        Assert.AreEqual(BitAiFieldType.DateTime, fields["at"].Type);
        Assert.IsFalse(fields["at"].Required);
    }

    [TestMethod]
    public void NestedObjectsArraysAndLocalReferencesAreFollowed()
    {
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "$defs": { "address": { "type": "object", "properties": { "city": { "type": "string" } }, "required": ["city"] } },
              "definitions": { "tag": { "type": "string", "maxLength": 12 } },
              "properties": {
                "home": { "$ref": "#/$defs/address", "title": "Home address" },
                "tags": { "type": "array", "items": { "$ref": "#/definitions/tag" }, "minItems": 1, "maxItems": 3 },
                "guests": { "type": "array", "items": { "type": "object", "properties": { "name": { "type": "string" }, "vip": { "type": "boolean" } } } },
                "loop": { "$ref": "#/properties/loop" }
              }
            }
            """).RootElement;

        var fields = BitAiFormDefinition.FromJsonSchema(schema).Fields.ToDictionary(f => f.Name);

        var home = fields["home"];
        Assert.AreEqual(BitAiFieldType.Object, home.Type);
        Assert.AreEqual("Home address", home.Label);
        Assert.AreEqual(1, home.Fields!.Count);
        Assert.IsTrue(home.Fields[0].Required);

        var tags = fields["tags"];
        Assert.AreEqual(BitAiFieldType.Array, tags.Type);
        Assert.AreEqual(12, tags.Items!.MaxLength);
        Assert.AreEqual((1, 3), (tags.MinItems, tags.MaxItems));

        var guests = fields["guests"];
        Assert.AreEqual(BitAiFieldType.Object, guests.Items!.Type);
        CollectionAssert.AreEqual(new[] { "name", "vip" }, guests.Items.Fields!.Select(f => f.Name).ToArray());

        Assert.AreEqual(BitAiFieldType.Text, fields["loop"].Type);
    }

    [TestMethod]
    public void AnIntegerFieldRejectsFractions()
    {
        var definition = new BitAiFormDefinition
        {
            Fields = [new BitAiFormField("seats") { Type = BitAiFieldType.Number, Integer = true, MultipleOf = 2 }],
        };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));

        InputOf(form, "Seats").Change("1.5");
        Submit(form).Click();
        StringAssert.Contains(form.Find(".bit-aifrm-fld").TextContent, "whole number");

        InputOf(form, "Seats").Change("3");
        Submit(form).Click();
        StringAssert.Contains(form.Find(".bit-aifrm-fld").TextContent, "multiple of 2");

        InputOf(form, "Seats").Change("4");
        Submit(form).Click();
        Assert.IsFalse(form.Find(".bit-aifrm-fld").TextContent.Contains("must be"));
        Assert.IsNull(InputOf(form, "Seats").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void ControlsCarryTheAccessibilityAttributesTheirStateCallsFor()
    {
        var definition = new BitAiFormDefinition
        {
            Fields = [new BitAiFormField("guest name") { Required = true, Description = "As on the booking." }],
        };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));

        // The field is addressable by an id built from its path, sanitised to what an id selector accepts.
        var field = form.Find(".bit-aifrm-fld > *");
        Assert.IsTrue(Regex.IsMatch(field.Id!, "^[A-Za-z0-9_-]+$"), field.Id);
        StringAssert.StartsWith(field.Id, form.Instance.UniqueId);

        var input = form.Find("input");
        Assert.AreEqual(input.Id, form.Find("label").GetAttribute("for"));
        Assert.IsTrue(input.HasAttribute("required"));
        Assert.IsFalse(input.HasAttribute("aria-invalid"));
        var describedBy = input.GetAttribute("aria-describedby")!.Split(' ');
        Assert.IsTrue(describedBy.Any(id => form.Find($"[id='{id}']").TextContent.Contains("As on the booking.")));

        Submit(form).Click();

        input = form.Find("input");
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        describedBy = input.GetAttribute("aria-describedby")!.Split(' ');
        Assert.IsTrue(describedBy.Any(id => form.Find($"[id='{id}']").TextContent.Contains("Guest name is required.")));
        Assert.IsTrue(describedBy.Any(id => form.Find($"[id='{id}']").TextContent.Contains("As on the booking.")));
    }

    [TestMethod]
    public void ADifferentSchemaWithTheSameFieldCountIsSeededAfresh()
    {
        var first = new BitAiFormDefinition { Fields = [new BitAiFormField("city") { Value = "Oslo" }, new BitAiFormField("notes")] };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, first));
        form.FindAll("input")[1].Input("bring cake");
        Assert.AreEqual("Oslo", form.Instance.Values["city"]);

        var second = new BitAiFormDefinition { Fields = [new BitAiFormField("city") { Value = "Paris" }, new BitAiFormField("budget") { Type = BitAiFieldType.Number }] };
        form.Render(p => p.Add(c => c.Definition, second));

        Assert.AreEqual("Paris", form.Instance.Values["city"]);
        Assert.IsFalse(form.Instance.Values.ContainsKey("notes"));
        Assert.AreEqual("Paris", form.FindAll("input")[0].GetAttribute("value"));
    }

    [TestMethod]
    public void ARebuiltDefinitionOfTheSameShapeKeepsWhatWasTyped()
    {
        static BitAiFormDefinition Build() => BitAiFormDefinition.FromJsonSchema(JsonDocument.Parse("""
            { "type": "object", "properties": { "plan": { "type": "string", "enum": ["free", "pro"] }, "city": { "type": "string" } } }
            """).RootElement);

        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, Build()));

        // The select is a BitDropdown: opened, then the option picked.
        form.Find(".bit-drp-wrp").Click();
        form.FindAll(".bit-drp-itm").First(i => i.TextContent.Trim() == "pro").Click();
        InputOf(form, "City").Input("Oslo");

        form.Render(p => p.Add(c => c.Definition, Build()));

        Assert.AreEqual("pro", form.Instance.Values["plan"]);
        Assert.AreEqual("Oslo", form.Instance.Values["city"]);
    }

    [TestMethod]
    public void TheControlsFollowTheTypesOfTheFields()
    {
        var definition = new BitAiFormDefinition
        {
            Fields =
            [
                new BitAiFormField("name"),
                new BitAiFormField("bio") { Type = BitAiFieldType.MultilineText },
                new BitAiFormField("age") { Type = BitAiFieldType.Number },
                new BitAiFormField("vip") { Type = BitAiFieldType.Boolean },
                new BitAiFormField("plan") { Type = BitAiFieldType.Select, Options = ["free", "pro"] },
                new BitAiFormField("day") { Type = BitAiFieldType.Date },
                new BitAiFormField("at") { Type = BitAiFieldType.DateTime },
                new BitAiFormField("time") { Type = BitAiFieldType.Time },
                new BitAiFormField("mail") { Type = BitAiFieldType.Email },
                new BitAiFormField("site") { Type = BitAiFieldType.Url },
                new BitAiFormField("secret") { Type = BitAiFieldType.Password },
            ],
        };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));

        string[] expected = ["bit-tfl", "bit-tfl", "bit-nfl", "bit-chb", "bit-drp", "bit-dtp", "bit-dtp", "bit-tpc", "bit-tfl", "bit-tfl", "bit-tfl"];
        var fields = form.FindAll(".bit-aifrm-fld");

        Assert.AreEqual(expected.Length, fields.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.IsTrue(fields[i].Children[0].ClassList.Contains(expected[i]), $"Field {i} is not a {expected[i]}.");
        }
        Assert.AreEqual("url", InputOf(form, "Site").GetAttribute("type"));
        Assert.AreEqual("password", InputOf(form, "Secret").GetAttribute("type"));
    }

    [TestMethod]
    public void ACheckboxWritesItsValueAsBooleanText()
    {
        IReadOnlyDictionary<string, string?>? submitted = null;
        var definition = new BitAiFormDefinition { Fields = [new BitAiFormField("vip") { Type = BitAiFieldType.Boolean }] };
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, definition)
            .Add(c => c.OnSubmit, values => submitted = values));

        form.Find("input[type=checkbox]").Click();
        Submit(form).Click();

        Assert.AreEqual("true", submitted!["vip"]);
    }

    [TestMethod]
    public void DatesAndTimesAreReadAndWrittenInTheInvariantFormOfANativeInput()
    {
        var definition = new BitAiFormDefinition
        {
            Fields =
            [
                new BitAiFormField("day") { Type = BitAiFieldType.Date, Value = "2024-05-01" },
                new BitAiFormField("at") { Type = BitAiFieldType.DateTime, Value = "2024-05-01T10:30" },
                new BitAiFormField("time") { Type = BitAiFieldType.Time, Value = "10:30" },
            ],
        };
        JsonElement? json = null;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, definition)
            .Add(c => c.OnSubmitJson, j => json = j));

        var pickers = form.FindComponents<BitDatePicker>();
        Assert.AreEqual(new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero), pickers[0].Instance.Value);
        Assert.AreEqual(new DateTimeOffset(2024, 5, 1, 10, 30, 0, TimeSpan.Zero), pickers[1].Instance.Value);
        Assert.AreEqual(new TimeSpan(10, 30, 0), form.FindComponent<BitTimePicker>().Instance.Value);

        Submit(form).Click();

        Assert.AreEqual("2024-05-01", json!.Value.GetProperty("day").GetString());
        Assert.AreEqual("2024-05-01T10:30", json.Value.GetProperty("at").GetString());
        Assert.AreEqual("10:30", json.Value.GetProperty("time").GetString());
    }

    [TestMethod]
    public void SubmittingProducesJsonInTheShapeOfTheSchema()
    {
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "guests": { "type": "integer" },
                "vip": { "type": "boolean", "default": true },
                "contact": { "type": "object", "properties": { "email": { "type": "string", "format": "email" }, "phone": { "type": "string" } } },
                "tags": { "type": "array", "items": { "type": "string" }, "default": ["window"] },
                "people": { "type": "array", "items": { "type": "object", "properties": { "name": { "type": "string" }, "age": { "type": "number" } } } }
              }
            }
            """).RootElement;
        var definition = BitAiFormDefinition.FromJsonSchema(schema);
        JsonElement? json = null;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, definition)
            .Add(c => c.OnSubmitJson, j => json = j));

        InputOf(form, "Guests").Change("4");
        InputOf(form, "Email").Input("ada@example.com");
        // Re-queried after every event, because each one re-renders the tree.
        form.FindAll("button.bit-aifrm-add")[0].Click();                 // tags[1]
        form.Find("input[aria-label='Tags 2']").Input("quiet");
        form.FindAll("button.bit-aifrm-add")[1].Click();                 // people[0]
        InputOf(form, "Name").Input("Ada");
        InputOf(form, "Age").Change("36.5");
        Submit(form).Click();

        Assert.IsNotNull(json);
        var root = json!.Value;
        Assert.AreEqual(4, root.GetProperty("guests").GetInt32());
        Assert.IsTrue(root.GetProperty("vip").GetBoolean());
        Assert.AreEqual("ada@example.com", root.GetProperty("contact").GetProperty("email").GetString());
        Assert.IsFalse(root.GetProperty("contact").TryGetProperty("phone", out _));
        CollectionAssert.AreEqual(new[] { "window", "quiet" }, root.GetProperty("tags").EnumerateArray().Select(e => e.GetString()).ToArray());
        var people = root.GetProperty("people");
        Assert.AreEqual(1, people.GetArrayLength());
        Assert.AreEqual("Ada", people[0].GetProperty("name").GetString());
        Assert.AreEqual(36.5, people[0].GetProperty("age").GetDouble());
    }

    [TestMethod]
    public void RemovingAListEntryClosesTheGapAndItemBoundsAreEnforced()
    {
        var definition = new BitAiFormDefinition
        {
            Fields = [new BitAiFormField("tags") { Type = BitAiFieldType.Array, Items = new BitAiFormField("tag"), MinItems = 2, Value = """["a","b","c"]""" }],
        };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));
        Assert.AreEqual(3, form.FindAll(".bit-aifrm-itm").Count);
        Assert.AreEqual("Remove Tags 2", form.FindAll("button.bit-aifrm-rmv")[1].GetAttribute("aria-label"));

        form.FindAll("button.bit-aifrm-rmv")[1].Click();

        CollectionAssert.AreEqual(new[] { "a", "c" }, form.FindAll(".bit-aifrm-itm input").Select(i => i.GetAttribute("value")).ToArray());
        Assert.AreEqual("c", form.Instance.Values["tags[1]"]);
        Assert.IsFalse(form.Instance.Values.ContainsKey("tags[2]"));

        form.FindAll("button.bit-aifrm-rmv")[0].Click();
        Submit(form).Click();

        var error = form.Find(".bit-aifrm-err");
        StringAssert.Contains(error.TextContent, "at least 2 items");
        Assert.AreEqual("alert", error.GetAttribute("role"));
        Assert.IsTrue(form.Find("fieldset").ClassList.Contains("bit-aifrm-inv"));
        Assert.AreEqual(error.Id, form.Find("fieldset").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void AReadOnlyListOffersNoAddOrRemove()
    {
        var definition = new BitAiFormDefinition
        {
            Fields = [new BitAiFormField("tags") { Type = BitAiFieldType.Array, ReadOnly = true, Value = """["a"]""" }],
        };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));

        Assert.AreEqual(0, form.FindAll("button.bit-aifrm-rmv").Count);
        Assert.AreEqual(0, form.FindAll("button.bit-aifrm-add").Count);
    }

    [TestMethod]
    public void TheCancelButtonRaisesOnCancel()
    {
        var cancelled = false;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.OnCancel, () => cancelled = true));

        form.Find("button.bit-aifrm-cnl").Click();

        Assert.IsTrue(cancelled);
    }

    [TestMethod]
    public void ABusyFormTurnsItsActionsAway()
    {
        var submitted = false;
        var cancelled = false;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, new BitAiFormDefinition())
            .Add(c => c.Busy, true)
            .Add(c => c.OnSubmit, _ => submitted = true)
            .Add(c => c.OnCancel, () => cancelled = true));

        Assert.AreEqual("true", Submit(form).GetAttribute("aria-disabled"));

        Submit(form).Click();
        form.Find("button.bit-aifrm-cnl").Click();

        Assert.IsFalse(submitted);
        Assert.IsFalse(cancelled);
    }

    [TestMethod]
    public void ASubmittedFormShowsTheConfirmationInPlaceOfItsActions()
    {
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.Submitted, true));

        Assert.AreEqual(0, form.FindAll(".bit-aifrm-act").Count);
        StringAssert.Contains(form.Find(".bit-aifrm-sbm .bit-tag").TextContent, "Submitted");
        // A neutral tag through the host-default class, never a Color, so the app's --bit-Tag-* variables reach it.
        Assert.IsTrue(form.Find(".bit-aifrm-sbm .bit-tag").ClassList.Contains("bit-tag-dft-ter"));
        Assert.IsNull(form.FindComponent<BitTag>().Instance.Color);
    }

    [TestMethod]
    public void AFormWithoutLabelsUsesTheDefaultEnglishOnes()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, new BitAiFormDefinition()));

        Assert.AreEqual("Submit", Submit(form).TextContent.Trim());
        Assert.AreEqual("Cancel", form.Find("button.bit-aifrm-cnl").TextContent.Trim());
    }

    [TestMethod]
    public void AnEmptyCancelLabelHidesTheCancelButton()
    {
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, new BitAiFormDefinition { CancelLabel = "" }));

        Assert.AreEqual(0, form.FindAll("button.bit-aifrm-cnl").Count);
    }

    [TestMethod]
    public void NumberFieldsValidateIndependentlyOfTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            IReadOnlyDictionary<string, string?>? submitted = null;
            var definition = new BitAiFormDefinition
            {
                Fields = [new BitAiFormField("ratio") { Type = BitAiFieldType.Number, Maximum = 2 }],
            };
            var form = RenderComponent<BitAiForm>(p => p
                .Add(c => c.Definition, definition)
                .Add(c => c.OnSubmit, values => submitted = values));

            form.Find("input").Change("1.5");
            Submit(form).Click();

            Assert.IsFalse(form.Markup.Contains("must be"));
            Assert.AreEqual("1.5", submitted!["ratio"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [TestMethod]
    public void ADisabledFormDisablesItsControlsAndSubmitsNothing()
    {
        var submitted = false;
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.Disabled, true)
            .Add(c => c.OnSubmit, _ => submitted = true));

        Assert.IsTrue(form.Find(".bit-aifrm").ClassList.Contains("bit-dis"));
        Assert.IsTrue(form.FindAll("input, textarea").All(i => i.HasAttribute("disabled")));

        Submit(form).Click();

        Assert.IsFalse(submitted);
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiFormShouldRespectDir(BitDir? dir, string? expected)
    {
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.Dir, dir));

        var root = form.Find(".bit-aifrm");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitAiFormShouldRespectClassStyleIdAndTheClassStylesOfItsParts()
    {
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.Id, "booking")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Classes, new BitAiFormClassStyles { Root = "root-class", Title = "title-class", Field = "field-class" })
            .Add(c => c.Styles, new BitAiFormClassStyles { Root = "padding: 2px", Title = "color: red" }));

        var root = form.Find(".bit-aifrm");

        Assert.AreEqual("booking", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding: 2px");
        Assert.IsTrue(form.Find(".bit-aifrm-ttl").ClassList.Contains("title-class"));
        Assert.AreEqual("color: red", form.Find(".bit-aifrm-ttl").GetAttribute("style"));
        Assert.AreEqual(3, form.FindAll(".bit-aifrm-fld.field-class").Count);
        StringAssert.StartsWith(form.Find(".bit-aifrm-fld > *").Id, "booking-");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiFormShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var form = RenderComponent<BitAiForm>(p => p
            .Add(c => c.Definition, Definition)
            .Add(c => c.Visibility, visibility));

        var style = form.Find(".bit-aifrm").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }
}
