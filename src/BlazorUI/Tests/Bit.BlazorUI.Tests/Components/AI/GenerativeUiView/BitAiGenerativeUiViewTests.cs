using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.GenerativeUiView;

[TestClass]
public class BitAiGenerativeUiViewTests : BitAiTestBase
{
    private sealed class ForecastCard : ComponentBase
    {
        [Parameter] public string City { get; set; } = string.Empty;

        [Parameter] public int Degrees { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "forecast");
            builder.AddContent(2, $"{City}: {Degrees}");
            builder.CloseElement();
        }
    }

    private sealed class ListCard : ComponentBase
    {
        [Parameter] public int? Count { get; set; }

        [Parameter] public List<string>? Items { get; set; }
    }

    private sealed class GermanStrings : BitAiStrings
    {
        public override string UnreadableProperty(string name, string type) => $"{name} ist kein {type}";

        public override string MissingProperty(string name, string component) => $"{name} fehlt in {component}";
    }

    private sealed class ThrowingCard : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => throw new System.InvalidOperationException("boom");
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static BitAiGenerativeUiPart CompletedPart(string name, string props)
    {
        var part = new BitAiGenerativeUiPart(name);
        part.SetProps(Json(props));
        part.Complete();
        return part;
    }



    [TestMethod]
    public void ARegisteredComponentIsInstantiatedWithModelSuppliedProperties()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var part = new BitAiGenerativeUiPart("forecast_card");
        part.SetProps(Json("""{"city":"Oslo","degrees":12}"""));

        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, part));

        Assert.AreEqual("Oslo: 12", view.Find(".bit-aiguv-cnt .forecast").TextContent);
        Assert.HasCount(1, view.FindComponents<BitAiRendererBoundary>());
    }

    [TestMethod]
    public void IgnoredPropertiesAreListedApartFromErrors()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo","degrees":"warm","mood":"sunny"}"""))
            .Add(c => c.ShowBindingErrors, true));

        var ignored = view.FindAll(".bit-aiguv-ign");
        Assert.HasCount(1, ignored);
        Assert.AreEqual("Ignored: mood", ignored[0].TextContent.Trim());

        var errors = view.FindAll(".bit-aiguv-err");
        Assert.HasCount(1, errors);
        Assert.IsFalse(errors[0].TextContent.Contains("mood", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void IgnoredPropertiesAloneAreListedWhenBindingOtherwiseSucceeded()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo","degrees":12,"mood":"sunny"}"""))
            .Add(c => c.ShowBindingErrors, true));

        var ignored = view.FindAll(".bit-aiguv-ign");
        Assert.HasCount(1, ignored);
        Assert.AreEqual("Ignored: mood", ignored[0].TextContent.Trim());
        Assert.IsEmpty(view.FindAll(".bit-aiguv-err"));
    }

    [TestMethod]
    public void AValueThatDidNotBindIsWordedForThePageWithWhatTheSerializerSaidAsItsTitle()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo","degrees":"warm"}"""))
            .Add(c => c.ShowBindingErrors, true));

        var error = view.Find(".bit-aiguv-err");
        var detail = error.GetAttribute("title");

        Assert.AreEqual("'degrees' could not be read as Int32", error.TextContent.Trim());
        Assert.IsFalse(string.IsNullOrEmpty(detail));
        Assert.IsFalse(error.TextContent.Contains(detail!, System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void AMissingRequiredPropertyIsWordedForThePage()
    {
        Generative.Register<ForecastCard>("forecast_card", null, "City");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"degrees":12}"""))
            .Add(c => c.ShowBindingErrors, true));

        var error = view.Find(".bit-aiguv-err");

        Assert.AreEqual("'City' is required by 'forecast_card'", error.TextContent.Trim());
        Assert.IsNull(error.GetAttribute("title"));
    }

    [TestMethod]
    public void TheBindingFailuresComeFromTheStringTable()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { Strings = new GermanStrings() });
        Generative.Register<ForecastCard>("forecast_card", null, "City");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"degrees":"warm"}"""))
            .Add(c => c.ShowBindingErrors, true));

        CollectionAssert.AreEqual(new[] { "degrees ist kein Int32", "City fehlt in forecast_card" },
                                  view.FindAll(".bit-aiguv-err").Select(e => e.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void ANullableOrGenericTypeIsNamedTheWayAPersonReadsIt()
    {
        Generative.Register<ListCard>("list_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("list_card", """{"count":"many","items":"one"}"""))
            .Add(c => c.ShowBindingErrors, true));

        CollectionAssert.AreEquivalent(new[] { "'count' could not be read as Int32", "'items' could not be read as List" },
                                       view.FindAll(".bit-aiguv-err").Select(e => e.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void TheBindingDiagnosticsAreHiddenUnlessAskedFor()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo","degrees":"warm","mood":"sunny"}""")));

        Assert.IsEmpty(view.FindAll(".bit-aiguv-dgn"));
    }

    [TestMethod]
    public void AnUnregisteredComponentNameRendersAPlaceholderRatherThanThrowing()
    {
        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("definitely_not_registered", "{}")));

        var placeholder = view.Find(".bit-aiguv-unr");
        StringAssert.Contains(placeholder.TextContent, "definitely_not_registered");
        Assert.IsFalse(view.Markup.Contains("forecast", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void AnUnregisteredComponentNameSaysNothingWhileItsPropsStillStream()
    {
        var part = new BitAiGenerativeUiPart("not_yet");
        part.AppendProps("""{"a":""");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, part));

        Assert.IsEmpty(view.FindAll(".bit-aiguv-unr"));
        Assert.AreEqual(0, view.Find(".bit-aiguv").ChildElementCount);
    }

    [TestMethod]
    public void PartialPropertiesStillRender()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var part = new BitAiGenerativeUiPart("forecast_card");
        part.AppendProps("""{"city":"Osl""");

        var markup = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, part)).Markup;

        StringAssert.Contains(markup, "Osl: 0");
    }

    [TestMethod]
    public void TheComponentIsBoundAgainAsItsPropsStream()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var part = new BitAiGenerativeUiPart("forecast_card");
        part.AppendProps("""{"city":"Osl""");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, part));

        part.AppendProps("""o","degrees":7}""");
        view.Render(p => p.Add(c => c.Part, part));

        Assert.AreEqual("Oslo: 7", view.Find(".forecast").TextContent);
    }

    [TestMethod]
    public void AComponentThatThrowsIsContainedByTheBoundary()
    {
        Generative.Register<ThrowingCard>("throwing_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, CompletedPart("throwing_card", "{}")));

        Assert.HasCount(1, view.FindComponents<BitAiRendererBoundary>());
        Assert.IsNotNull(view.Find(".bit-aiguv"));
    }

    [TestMethod]
    public void BitAiGenerativeUiViewShouldRenderTheRootAndItsParts()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo","degrees":"warm","mood":"sunny"}"""))
            .Add(c => c.ShowBindingErrors, true)
            .Add(c => c.Id, "g1")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "c-root", Content = "c-content", Diagnostics = "c-dgn", Error = new() { Root = "c-error" }, Ignored = new() { Root = "c-ignored" } }));

        var root = view.Find(".bit-aiguv");

        Assert.AreEqual("g1", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.IsTrue(view.Find(".bit-aiguv-cnt").ClassList.Contains("c-content"));
        Assert.IsTrue(view.Find(".bit-aiguv-dgn").ClassList.Contains("c-dgn"));
        Assert.IsTrue(view.Find(".bit-aiguv-err").ClassList.Contains("c-error"));
        Assert.IsTrue(view.Find(".bit-aiguv-ign").ClassList.Contains("c-ignored"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiGenerativeUiViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var view = RenderComponent<BitAiGenerativeUiView>(p => p
            .Add(c => c.Part, CompletedPart("x", "{}"))
            .Add(c => c.Visibility, visibility));

        var style = view.Find(".bit-aiguv").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiGenerativeUiViewShouldTakeTheCascadedParameters()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiGenerativeUiViewParams { Class = "cascaded", ShowBindingErrors = true }
            });
            p.AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, CompletedPart("forecast_card", """{"city":"Oslo","mood":"sunny"}""")));
        });

        Assert.IsTrue(component.Find(".bit-aiguv").ClassList.Contains("cascaded"));
        Assert.HasCount(1, component.FindAll(".bit-aiguv-ign"));
    }

    [TestMethod]
    public void BitAiGenerativeUiViewShouldKeepItsOwnShowBindingErrorsOverACascadedOne()
    {
        Generative.Register<ForecastCard>("forecast_card");

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams> { new BitAiGenerativeUiViewParams { ShowBindingErrors = true } });
            p.AddChildContent<BitAiGenerativeUiView>(c => c
                .Add(x => x.Part, CompletedPart("forecast_card", """{"city":"Oslo","mood":"sunny"}"""))
                .Add(x => x.ShowBindingErrors, false));
        });

        Assert.IsEmpty(component.FindAll(".bit-aiguv-dgn"));
    }

    [TestMethod]
    public void BitAiGenerativeUiViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiGenerativeUiView", BitAiGenerativeUiViewParams.ParamName);
        Assert.AreEqual(BitAiGenerativeUiViewParams.ParamName, new BitAiGenerativeUiViewParams().Name);
    }
}
