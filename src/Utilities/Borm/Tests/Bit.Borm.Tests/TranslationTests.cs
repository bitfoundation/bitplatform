using System.Globalization;
using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// The library's own words in the reader's language: every table says everything English says, with the same
/// placeholders, and the form's language - or else the UI culture - picks the table.
/// </summary>
[TestClass]
public class TranslationTests : ComponentTestBase
{
    private static readonly BormTranslatedMessageProvider Translated = BormTranslatedMessageProvider.Instance;

    /// <summary>Every word English has: each message key, and each designer property and kind.</summary>
    private static IEnumerable<string> EnglishKeys()
    {
        foreach (var constant in typeof(BormMessageKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (constant is { IsLiteral: true } && constant.GetRawConstantValue() is string key) yield return key;
        }
        foreach (var property in BormDefaultMessageProvider.DesignerProperties.Keys) yield return BormMessageKeys.DesignerProperty(property);
        foreach (var kind in BormDefaultMessageProvider.DesignerKinds.Keys) yield return BormMessageKeys.DesignerKind(kind);
    }

    // Arguments no translation could say by accident, so where each one lands can be seen.
    private static readonly object?[] Marks = ["\u0001A", "\u0001B", "\u0001C", "\u0001D"];

    private static string[] MarksIn(string text) => Marks.Cast<string>().Where(m => text.Contains(m, StringComparison.Ordinal)).ToArray();

    private static IServiceProvider Translating() => new ServiceCollection().AddBormTranslations().BuildServiceProvider();

    [TestMethod]
    public void Every_table_says_every_word_English_has()
    {
        var english = EnglishKeys().ToHashSet(StringComparer.Ordinal);
        Assert.IsGreaterThan(150, english.Count, "the reflection found the keys");

        foreach (var language in BormTranslatedMessageProvider.Languages)
        {
            var table = BormTranslations.Tables[language];
            var missing = english.Where(k => !table.ContainsKey(k)).ToList();
            Assert.IsEmpty(missing, $"{language} lacks {string.Join(", ", missing)}");

            // And nothing English does not know - a misspelt key would never be asked for.
            var unknown = table.Keys
                .Where(k => !english.Contains(k) && !(k.EndsWith(".one", StringComparison.Ordinal) && english.Contains(k[..^4])))
                .ToList();
            Assert.IsEmpty(unknown, $"{language} has words for nothing: {string.Join(", ", unknown)}");
        }
    }

    // English makes the item noun plural by adding an s, which no other language can do to it: these say "entries"
    // in their own word for it instead of the noun.
    private static readonly HashSet<string> MayLeaveOut = [BormMessageKeys.ArrayCount, BormMessageKeys.ArrayEmpty, BormMessageKeys.ArrayElsewhere];

    [TestMethod]
    public void Every_translation_says_the_same_arguments_as_English()
    {
        var wrong = new List<string>();
        foreach (var language in BormTranslatedMessageProvider.Languages)
        {
            foreach (var key in EnglishKeys())
            {
                var expected = MarksIn(BormDefaultMessageProvider.Instance.Get(key, Marks));
                // A brace out of place throws here, which is what a reader would otherwise have met at runtime.
                var actual = MarksIn(Translated.Get(language, key, Marks));
                var right = !actual.Except(expected).Any() && (actual.Length == expected.Length || MayLeaveOut.Contains(key));
                if (!right) wrong.Add($"{language} {key}: says {actual.Length} of {expected.Length}");
            }
        }
        Assert.IsEmpty(wrong, string.Join(Environment.NewLine, wrong));
    }

    [TestMethod]
    public void A_wording_for_exactly_one_is_used_for_one_and_only_for_one()
    {
        Assert.AreEqual("Fügen Sie mindestens einen Eintrag hinzu.", Translated.Get("de", BormMessageKeys.MinItems, 1));
        Assert.Contains("3", Translated.Get("de", BormMessageKeys.MinItems, 3), StringComparison.Ordinal);

        // Every one-variant formats with the arguments its key is given.
        foreach (var language in BormTranslatedMessageProvider.Languages)
        {
            foreach (var key in BormTranslations.Tables[language].Keys.Where(k => k.EndsWith(".one", StringComparison.Ordinal)))
                Assert.IsFalse(string.IsNullOrWhiteSpace(Translated.Get(language, key[..^4], 1, Marks[1], Marks[2])), $"{language} {key}");
        }
    }

    [TestMethod]
    public void A_regional_culture_reads_its_language_and_an_unknown_one_reads_English()
    {
        Assert.AreEqual(Translated.Get("de", BormMessageKeys.Required), Translated.Get("de-CH", BormMessageKeys.Required));
        Assert.AreEqual(Translated.Get("pt", BormMessageKeys.Submit), Translated.Get("pt-BR", BormMessageKeys.Submit));
        Assert.AreEqual(Translated.Get("fr", BormMessageKeys.Submit), Translated.Get("FR-ca", BormMessageKeys.Submit));
        Assert.AreNotEqual("Submit", Translated.Get("de", BormMessageKeys.Submit));

        Assert.AreEqual("Submit", Translated.Get("sw-KE", BormMessageKeys.Submit));
        Assert.AreEqual("Submit", Translated.Get(string.Empty, BormMessageKeys.Submit));
        // A key no table has comes back the way English answers it.
        Assert.AreEqual("no.such.key", Translated.Get("de", "no.such.key"));
    }

    [TestMethod]
    public void Simplified_chinese_is_given_only_to_cultures_that_write_it()
    {
        var simplified = Translated.Get("zh", BormMessageKeys.Submit);
        Assert.AreNotEqual("Submit", simplified);
        foreach (var culture in (string[])["zh-Hans", "zh-CN", "zh-SG", "zh-Hans-CN"])
            Assert.AreEqual(simplified, Translated.Get(culture, BormMessageKeys.Submit), culture);

        // Each of these is parented by zh, whose table is simplified: English reads better than the wrong script.
        foreach (var culture in (string[])["zh-TW", "zh-HK", "zh-MO", "zh-Hant", "zh-Hant-TW"])
            Assert.AreEqual("Submit", Translated.Get(culture, BormMessageKeys.Submit), culture);
    }

    [TestMethod]
    public void Without_a_language_of_its_own_the_provider_follows_the_ui_culture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            Assert.AreEqual(Translated.Get("es", BormMessageKeys.Submit), Translated.Get(BormMessageKeys.Submit));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public async Task The_forms_language_decides_the_language_of_its_messages_and_buttons()
    {
        Services.AddBormTranslations();
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services) { Language = "fr" };
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        Assert.AreEqual("fr", cut.Find("form").GetAttribute("lang"));
        Assert.AreEqual(Translated.Get("fr", BormMessageKeys.Submit), cut.Find("button[type=submit]").TextContent.Trim());

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(Translated.Get("fr", BormMessageKeys.Required), cut.Find(".bit-borm-message--error").TextContent);
        // The severity, said before the message for a screen reader, is in the same language.
        Assert.AreEqual(Translated.Get("fr", BormMessageKeys.SeverityError), cut.Find(".bit-borm-message--error").GetAttribute("data-severity"));

        // Another language rewrites the form's words at once, and a rule's verdict when it is next given.
        cut.InvokeAsync(() => state.Language = "ja").Wait();
        Assert.AreEqual(Translated.Get("ja", BormMessageKeys.Submit), cut.Find("button[type=submit]").TextContent.Trim());
        await cut.InvokeAsync(() => state.ValidateAsync().AsTask());
        Assert.AreEqual(Translated.Get("ja", BormMessageKeys.Required), cut.Find(".bit-borm-message--error").TextContent);
    }

    [TestMethod]
    public async Task A_state_the_page_built_without_services_speaks_with_the_apps()
    {
        // new BormState(definition, data) is the shorter way to write it, and the registered provider still applies.
        Services.AddBormTranslations();
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor()) { Language = "nl" };
        Assert.AreEqual("Submit", state.Text(BormMessageKeys.Submit), "nothing to look in yet");

        var cut = Render<BormView>(p => p.Add(x => x.State, state));
        await cut.Find("form").SubmitAsync();

        Assert.IsNotNull(state.Services);
        Assert.AreEqual(Translated.Get("nl", BormMessageKeys.Submit), cut.Find("button[type=submit]").TextContent.Trim());
        Assert.AreEqual(Translated.Get("nl", BormMessageKeys.Required), cut.Find(".bit-borm-message--error").TextContent);
    }

    [TestMethod]
    public void A_state_given_services_of_its_own_keeps_them()
    {
        Services.AddBormTranslations();
        var own = new ServiceCollection().BuildServiceProvider();
        var state = new BormState(BormBuilder.Create().Text("name").Build(), new BormDictionaryDataAccessor(), own) { Language = "nl" };

        Render<BormView>(p => p.Add(x => x.State, state));

        Assert.AreSame(own, state.Services);
        Assert.AreEqual("Submit", state.Text(BormMessageKeys.Submit));
    }

    [TestMethod]
    public void A_refused_entry_is_described_again_in_the_new_language_straight_away()
    {
        var form = BormBuilder.Create().Integer("age").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Translating()) { Language = "de" };

        state.SetValue("age", "zwölf");
        Assert.AreEqual(Translated.Get("de", BormMessageKeys.Conversion, "zwölf"), state.MessagesFor("age").Single().Message);

        state.Language = "es";

        var message = state.MessagesFor("age").Single();
        Assert.AreEqual(Translated.Get("es", BormMessageKeys.Conversion, "zwölf"), message.Message);
        Assert.AreEqual(BormValidationSource.Conversion, message.Source);
    }

    [TestMethod]
    public async Task A_rules_verdict_already_on_screen_is_said_again_in_the_new_language()
    {
        // Left for the field's next validation, the message stayed English under a German label.
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Translating());
        await state.ValidateAsync();
        Assert.AreEqual("This field is required.", state.MessagesFor("name").Single().Message);

        state.Language = "de";

        await WaitUntil(() => state.MessagesFor("name").Single().Message != "This field is required.");
        Assert.AreEqual(Translated.Get("de", BormMessageKeys.Required), state.MessagesFor("name").Single().Message);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
    }

    [TestMethod]
    public async Task A_validator_used_on_its_own_answers_in_the_language_it_is_given()
    {
        // Validating a posted form on the server, in the language of the person who filled it in.
        var form = BormBuilder.Create().Text("name", f => f.Required().MaxLength(3)).Build();
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["name"] = "Grace" });
        var validator = new BormValidator { Language = "ru" };

        var messages = await validator.ValidateAsync(form, data, Translating());

        Assert.AreEqual(Translated.Get("ru", BormMessageKeys.MaxLength, 3), messages.Single().Message);
    }

    private sealed class PatternRule : IBormValidationRule
    {
        public string? Language { get; private set; }

        public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext context)
        {
            Language = context.Language;
            return new(BormRuleResult.Fail(context.Message(BormMessageKeys.Pattern)));
        }
    }

    [TestMethod]
    public async Task A_custom_rule_asking_for_a_built_in_message_gets_it_in_the_forms_language()
    {
        var rule = new PatternRule();
        var form = BormBuilder.Create().Text("code", f => f.Validate(rule)).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Translating()) { Language = "tr" };

        await state.ValidateAsync();

        Assert.AreEqual("tr", rule.Language);
        Assert.AreEqual(Translated.Get("tr", BormMessageKeys.Pattern), state.MessagesFor("code").Single().Message);
    }

    [TestMethod]
    public void A_provider_registered_first_is_kept()
    {
        var mine = new BormDefaultMessageProvider();
        var services = new ServiceCollection().AddBormMessages(mine).AddBormTranslations().BuildServiceProvider();

        Assert.AreSame(mine, services.GetRequiredService<IBormMessageProvider>());
    }

    [TestMethod]
    public void A_provider_that_cannot_choose_a_language_is_asked_as_before()
    {
        // The English defaults know no languages: a form's language changes nothing about what they say.
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor()) { Language = "de" };

        Assert.AreEqual("Submit", state.Text(BormMessageKeys.Submit));
    }

    [TestMethod]
    public void The_designer_speaks_the_language_it_is_given_and_changes_when_told()
    {
        Services.AddBormTranslations();
        var cut = Render<BormDesigner>(p => p.Add(x => x.Language, "ja"));

        Assert.AreEqual("ja", cut.Find(".bit-borm-designer").GetAttribute("lang"));
        Assert.Contains(h => h.TextContent == Translated.Get("ja", BormMessageKeys.DesignerAdd), cut.FindAll(".bit-borm-designer__heading"));
        Assert.Contains(b => b.TextContent == Translated.Get("ja", BormMessageKeys.DesignerKind("email")), cut.FindAll(".bit-borm-designer__kinds button"));
        // The form's own settings are a Borm form too, labelled in the same language.
        Assert.Contains(Translated.Get("ja", BormMessageKeys.DesignerProperty("title")), cut.Markup, StringComparison.Ordinal);

        cut.Render(p => p.Add(x => x.Language, "it"));

        Assert.Contains(h => h.TextContent == Translated.Get("it", BormMessageKeys.DesignerAdd), cut.FindAll(".bit-borm-designer__heading"));
        Assert.Contains(Translated.Get("it", BormMessageKeys.DesignerProperty("title")), cut.Markup, StringComparison.Ordinal);
    }
}
