using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ModelSelector;

[TestClass]
public class BitAiModelSelectorTests : BitAiTestBase
{
    private static readonly BitAiModel Alpha = new("alpha", "Alpha") { Family = "A series", Description = "The fast one", Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Vision };
    private static readonly BitAiModel Beta = new("beta", "Beta") { Family = "B series" };
    private static readonly BitAiModel Old = new("old", "Old") { Family = "A series", IsDeprecated = true };

    private static string? SelectedValue(IRenderedComponent<BitAiModelSelector> picker) =>
        picker.Instance.Selected?.ModelId;

    private static IReadOnlyList<string> OptionTexts(IRenderedComponent<BitAiModelSelector> picker) =>
        picker.FindAll(".bit-drp-itm").Select(o => o.TextContent.Trim()).ToList();

    [TestMethod]
    public void BitAiModelSelectorShouldRenderItsRootClassClassStyleAndDir()
    {
        var picker = RenderComponent<BitAiModelSelector>(parameters =>
        {
            parameters.Add(p => p.Models, [Alpha, Beta]);
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "margin: 1px");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Classes, new BitAiModelSelectorClassStyles { Root = "root-class" });
        });

        var root = picker.Find(".bit-aimds");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsNotNull(picker.Find(".bit-aimds > .bit-drp.bit-aimds-drp"));
    }

    [TestMethod]
    public void BitAiModelSelectorShouldRespectDisabled()
    {
        var picker = RenderComponent<BitAiModelSelector>(parameters =>
        {
            parameters.Add(p => p.Models, [Alpha, Beta]);
            parameters.Add(p => p.Disabled, true);
        });

        Assert.IsTrue(picker.Find(".bit-aimds").ClassList.Contains("bit-dis"));
        Assert.IsTrue(picker.Find(".bit-drp").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void ThePickerIsNamedAndDescribesTheSelectedModel()
    {
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.ModelId, "alpha"));

        Assert.AreEqual("The fast one", picker.FindComponent<BitDropdown<BitDropdownItem<string>, string>>().Instance.Title);
        Assert.AreEqual("Model", picker.FindComponent<BitDropdown<BitDropdownItem<string>, string>>().Instance.AriaLabel);
        Assert.AreEqual("The fast one", picker.FindAll(".bit-drp-itm").Single(o => o.TextContent.Trim() == "Alpha").GetAttribute("title"));
    }

    [TestMethod]
    public void TheEffectiveSelectionIsRaisedWhenNothingIsBound()
    {
        string? bound = null;
        BitAiModel? selected = null;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.ModelIdChanged, id => bound = id)
            .Add(c => c.OnModelSelected, m => selected = m));

        picker.WaitForAssertion(() => Assert.AreEqual("alpha", bound));
        Assert.AreSame(Alpha, selected);
        Assert.AreEqual("alpha", SelectedValue(picker));
        Assert.AreEqual("Alpha", picker.Find(".bit-drp-tdp").TextContent.Trim());
    }

    [TestMethod]
    public void ABoundModelThatIsInTheListIsNotReannounced()
    {
        var changes = 0;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.ModelId, "beta")
            .Add(c => c.ModelIdChanged, _ => changes++));

        picker.Render(p => p.Add(c => c.ModelId, "beta"));

        Assert.AreEqual(0, changes);
        Assert.AreEqual("beta", SelectedValue(picker));
        Assert.AreEqual("Beta", picker.Find(".bit-drp-tdp").TextContent.Trim());
    }

    [TestMethod]
    public void ADeprecatedBoundModelFallsBackAndTheChangeIsRaised()
    {
        string? bound = null;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Old, Alpha])
            .Add(c => c.ModelId, "old")
            .Add(c => c.ModelIdChanged, id => bound = id));

        picker.WaitForAssertion(() => Assert.AreEqual("alpha", bound));
        Assert.DoesNotContain("Old", OptionTexts(picker));
        Assert.DoesNotContain("Old (deprecated)", OptionTexts(picker));
    }

    [TestMethod]
    public void DeprecatedModelsAreShownAndLabelledOnlyOnRequest()
    {
        string? bound = null;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Old, Alpha])
            .Add(c => c.ModelId, "old")
            .Add(c => c.ShowDeprecated, true)
            .Add(c => c.ModelIdChanged, id => bound = id));

        Assert.Contains("Old (deprecated)", OptionTexts(picker));
        Assert.IsNull(bound);
    }

    [TestMethod]
    public void RequiredCapabilitiesFilterTheListAndMoveTheSelection()
    {
        string? bound = null;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Beta, Alpha])
            .Add(c => c.ModelId, "beta")
            .Add(c => c.RequiredCapabilities, BitAiModelCapabilities.Vision)
            .Add(c => c.ModelIdChanged, id => bound = id));

        Assert.AreEqual("Alpha", Assert.ContainsSingle(OptionTexts(picker)));
        picker.WaitForAssertion(() => Assert.AreEqual("alpha", bound));
    }

    [TestMethod]
    public void ModelsAreGroupedByFamilyThenProvider()
    {
        var gamma = new BitAiModel("gamma", "Gamma") { ProviderId = "acme" };
        var loose = new BitAiModel("delta", "Delta");
        var picker = RenderComponent<BitAiModelSelector>(p => p.Add(c => c.Models, [Alpha, Beta, gamma, loose]));

        var groups = picker.FindAll(".bit-drp-ihd").Select(g => g.TextContent.Trim()).ToList();

        CollectionAssert.AreEqual(new[] { "A series", "B series", "acme", "Other" }, groups);
    }

    [TestMethod]
    public void ModelsWithoutAGroupGetNoHeaders()
    {
        var picker = RenderComponent<BitAiModelSelector>(p => p.Add(c => c.Models, [new BitAiModel("one", "One"), new BitAiModel("two", "Two")]));

        Assert.IsEmpty(picker.FindAll(".bit-drp-ihd"));
        CollectionAssert.AreEqual(new[] { "One", "Two" }, OptionTexts(picker).ToArray());
    }

    // A dropdown handed new items is a dropdown whose items changed: the picker hands it new ones only when they read
    // differently, not on every render of its parent.
    [TestMethod]
    public void TheDropdownIsHandedNewOptionsOnlyWhenTheyReadDifferently()
    {
        IReadOnlyList<BitAiModel> models = [Alpha, Beta, Old];
        var picker = RenderComponent<BitAiModelSelector>(p => p.Add(c => c.Models, models));
        object? Items() => picker.FindComponent<BitDropdown<BitDropdownItem<string>, string>>().Instance.Items;
        var first = Items();

        picker.Render(p => p.Add(c => c.Models, models).Add(c => c.ModelId, "beta"));
        Assert.AreSame(first, Items());
        Assert.AreEqual("beta", SelectedValue(picker));

        picker.Render(p => p.Add(c => c.Models, models).Add(c => c.ShowDeprecated, true));
        Assert.AreNotSame(first, Items());
        Assert.Contains("Old (deprecated)", OptionTexts(picker));

        var shown = Items();
        picker.Render(p => p.Add(c => c.Models, [Alpha]).Add(c => c.ShowDeprecated, true));
        Assert.AreNotSame(shown, Items());
        CollectionAssert.AreEqual(new[] { "Alpha" }, OptionTexts(picker).ToArray());
    }

    // A host that keeps one list and replaces a model in it, leaving its count as it was, has the new model offered and
    // selected - not the one it replaced announced over the value it bound.
    [TestMethod]
    public void AModelReplacedInPlaceIsOffered()
    {
        var bound = new List<string>();
        var models = new List<BitAiModel> { Alpha, Beta };
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, models)
            .Add(c => c.ModelId, "alpha")
            .Add(c => c.ModelIdChanged, id => bound.Add(id)));

        var gamma = new BitAiModel("gamma", "Gamma") { Family = "A series" };
        models[0] = gamma;
        picker.Render(p => p.Add(c => c.Models, models).Add(c => c.ModelId, "gamma"));

        CollectionAssert.AreEqual(new[] { "Gamma", "Beta" }, OptionTexts(picker).ToArray());
        Assert.AreSame(gamma, picker.Instance.Selected);
        Assert.IsEmpty(bound);
    }

    // Strings that read the culture, or a localizer, as they are asked are new text in the same object: the options are
    // labelled with what they read now, as the rest of the chat is.
    [TestMethod]
    public void TheOptionsFollowStringsThatReadDifferentlyNow()
    {
        var strings = new SwitchableStrings();
        var loose = new BitAiModel("delta", "Delta");
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Old, loose])
            .Add(c => c.ShowDeprecated, true)
            .Add(c => c.ModelId, "alpha")
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Strings = strings }));

        Assert.Contains("Old (deprecated)", OptionTexts(picker));
        Assert.Contains("Other", picker.FindAll(".bit-drp-ihd").Select(g => g.TextContent.Trim()).ToList());

        strings.German = true;
        picker.Render(p => p.Add(c => c.ModelId, "alpha"));

        Assert.Contains("Old (veraltet)", OptionTexts(picker));
        Assert.Contains("Andere", picker.FindAll(".bit-drp-ihd").Select(g => g.TextContent.Trim()).ToList());
    }

    [TestMethod]
    public void TheSelectionIsAnnouncedOnceNotOnEveryRender()
    {
        var selections = 0;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.OnModelSelected, _ => selections++));

        picker.WaitForAssertion(() => Assert.AreEqual(1, selections));
        picker.Render(p => p.Add(c => c.Models, [Alpha, Beta]));
        picker.Render(p => p.Add(c => c.Models, [Alpha, Beta]));

        Assert.AreEqual(1, selections);
    }

    // A parent that takes the fallback up and later clears its value again is told the fallback again, so the bound value
    // never stays null while the picker shows a model.
    [TestMethod]
    public void ABoundValueClearedAgainIsFilledInAgain()
    {
        var changes = new List<string>();
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.ModelIdChanged, id => changes.Add(id)));

        picker.WaitForAssertion(() => Assert.HasCount(1, changes));

        // The parent takes the value up, as a binding does, and clears it later.
        picker.Render(p => p.Add(c => c.ModelId, "alpha"));
        picker.Render(p => p.Add(c => c.ModelId, (string?)null));

        picker.WaitForAssertion(() => Assert.HasCount(2, changes));
        Assert.AreEqual("alpha", changes[1]);
    }

    [TestMethod]
    public void ChoosingAnOptionRaisesBothEvents()
    {
        string? bound = null;
        BitAiModel? selected = null;
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Alpha, Beta])
            .Add(c => c.ModelId, "alpha")
            .Add(c => c.ModelIdChanged, id => bound = id)
            .Add(c => c.OnModelSelected, m => selected = m));

        picker.FindAll(".bit-drp-itm").Single(o => o.TextContent.Trim() == "Beta").Click();

        Assert.AreEqual("beta", bound);
        Assert.AreSame(Beta, selected);
        Assert.AreEqual("beta", SelectedValue(picker));
    }

    [TestMethod]
    public void TheRegisteredCatalogIsUsedWhenNoModelsAreGiven()
    {
        var catalog = new TestCatalog([Alpha, Beta], Beta);

        var picker = RenderComponent<BitAiModelSelector>(p => p.Add(c => c.Catalog, catalog));

        CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, OptionTexts(picker).ToArray());
        Assert.AreEqual("beta", SelectedValue(picker));
    }

    [TestMethod]
    public void NothingRendersWhenEveryModelIsFilteredOut()
    {
        var picker = RenderComponent<BitAiModelSelector>(p => p
            .Add(c => c.Models, [Beta])
            .Add(c => c.RequiredCapabilities, BitAiModelCapabilities.Vision));

        Assert.IsEmpty(picker.FindAll(".bit-aimds"));
        Assert.IsEmpty(picker.FindAll(".bit-drp"));
    }

    private sealed class SwitchableStrings : BitAiStrings
    {
        public bool German { get; set; }

        public override string OtherModels => German ? "Andere" : "Other";

        public override string ModelDeprecated(string name) => German ? $"{name} (veraltet)" : $"{name} (deprecated)";
    }

    private sealed class TestCatalog(IReadOnlyList<BitAiModel> models, BitAiModel? defaultModel) : IBitAiModelCatalog
    {
        public IReadOnlyList<BitAiModel> Models => models;

        public IReadOnlyList<BitAiProvider> Providers => [];

        public BitAiModel? DefaultModel => defaultModel;

        public BitAiModel? FindModel(string modelId) => models.FirstOrDefault(m => m.ModelId == modelId);

        public BitAiProvider? FindProvider(string providerId) => null;
    }
}
