using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.OptionsProvider;

/// <summary>
/// The user-facing text of the AI components comes from the string table of the options in force - the registered ones
/// here, the cascaded ones of a <see cref="BitAiOptionsProvider"/> in the last test.
/// </summary>
/// <remarks>
/// The one port of the original LocalisationTests: the tests of the components themselves read their words from a
/// cascaded table, these from the registered one.
/// </remarks>
[TestClass]
public class BitAiLocalisationTests : BitAiTestBase
{
    private sealed class GermanStrings : BitAiStrings
    {
        public override string AgentCompleted => "Fertig";

        public override string Submitted => "Gesendet";

        public override string Submit => "Absenden";

        public override string Today => "Heute";

        public override string Transcript => "Unterhaltung";

        public override string Conversations => "Unterhaltungen";

        public override string Copy => "Kopieren";

        public override string FieldRequired(string label) => $"{label} fehlt.";

        public override string CitationLabel(int? number, string title) => $"Quelle {number}: {title}";

        public override string BackToReference(int number) => $"Zurück zu Verweis {number}";
    }

    private static readonly BitAiUiOptions German = BitAiUiOptions.Default with { Strings = new GermanStrings() };

    [TestInitialize]
    public void RegisterGermanStrings() => Services.AddSingleton(German);

    [TestMethod]
    public void AgentStatusComesFromTheStringTable()
    {
        var badge = RenderComponent<BitAiAgentStatusBadge>(p => p.Add(c => c.Status, BitAiAgentRunStatus.Completed));

        StringAssert.Contains(badge.Markup, "Fertig");
    }

    [TestMethod]
    public void FormLabelsAndValidationComeFromTheStringTable()
    {
        var definition = new BitAiFormDefinition { Fields = [new BitAiFormField("city") { Required = true }] };
        var form = RenderComponent<BitAiForm>(p => p.Add(c => c.Definition, definition));

        var submit = form.Find("button.bit-aifrm-sbt");
        Assert.AreEqual("Absenden", submit.TextContent.Trim());

        submit.Click();
        StringAssert.Contains(form.Find(".bit-aifrm-fld").TextContent, "City fehlt.");

        form.Render(p => p.Add(c => c.Submitted, true));
        StringAssert.Contains(form.Find(".bit-aifrm-sbm").TextContent, "Gesendet");
    }

    [TestMethod]
    public void ConversationGroupsComeFromTheStringTable()
    {
        var list = RenderComponent<BitAiConversationList>(p => p.Add(
            c => c.Conversations, [new BitAiConversationSummary("c1", "Trip", DateTimeOffset.Now)]));

        Assert.AreEqual("Heute", list.Find(".bit-aicvl-grp").TextContent);
        Assert.AreEqual("Unterhaltungen", list.Find("nav").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void AccessibleNamesComeFromTheStringTable()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "Handbook")) { Number = 2 };
        var marker = RenderComponent<BitAiCitationMarker>(p => p.Add(c => c.Citation, citation));

        // The transcript rendered the way an interactive circuit renders it, as the original test set up.
        Interactive();
        var transcript = RenderComponent<BitAiMessageList>(p => p.Add(c => c.Conversation, new BitAiConversation()));

        Assert.AreEqual("Quelle 2: Handbook", marker.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual("Unterhaltung", transcript.Find("[role=log]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void FootnoteBackLinksComeFromTheStringTable()
    {
        var view = RenderComponent<BitAiMarkdownView>(p => p.Add(c => c.Document, BitAiMarkdownParser.Parse("Claim[^1].\n\n[^1]: The note.")));

        Assert.AreEqual("Zurück zu Verweis 1", view.Find("a.bit-aimdv-fnb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void TheStringTableOfAnOptionsProviderWinsOverTheRegisteredOne()
    {
        var provider = RenderComponent<BitAiOptionsProvider>(p => p
            .Add(c => c.UiOptions, BitAiUiOptions.Default)
            .AddChildContent<BitAiCopyButton>(b => b.Add(c => c.Content, "x")));

        Assert.AreEqual("Copy", provider.Find("button").GetAttribute("aria-label"));

        var registered = RenderComponent<BitAiCopyButton>(p => p.Add(c => c.Content, "x"));

        Assert.AreEqual("Kopieren", registered.Find("button").GetAttribute("aria-label"));
    }
}
