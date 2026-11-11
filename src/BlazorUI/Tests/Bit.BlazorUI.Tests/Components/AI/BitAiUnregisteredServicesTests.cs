using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Components.AI.Core;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI;

/// <summary>
/// The components with nothing of the AI packages registered - only the core's services, as an app that lazy-loads
/// Bit.BlazorUI.AI (BlazorWebAssemblyLazyLoad) has them, since calling AddBitBlazorUIAIServices at startup would load the
/// assembly there. Everything the registration gives them is handed over on the page instead: a session or a
/// BitAiServices (the transport, the tools, the renderers, the generative UI registry, the model catalog), and the
/// UI options.
/// </summary>
[TestClass]
public class BitAiUnregisteredServicesTests : BunitTestContext
{
    [TestInitialize]
    public void AddCoreServicesOnly()
    {
        Context.Services.AddBitBlazorUIServices();
    }

    private sealed class ForecastCard : ComponentBase
    {
        [Parameter] public string City { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "forecast");
            builder.AddContent(2, City);
            builder.CloseElement();
        }
    }

    private sealed class LazyStrings : BitAiStrings
    {
        public override string EmptyTitle => "Ask the lazy chat";
    }

    private static BitAiGenerativeUiPart CompletedPart(string name, string props)
    {
        using var document = JsonDocument.Parse(props);
        var part = new BitAiGenerativeUiPart(name);
        part.SetProps(document.RootElement.Clone());
        part.Complete();
        return part;
    }

    private static BitAiToolRegistry WeatherTools() => new([BitAiTool.Create(
        new BitAiToolDefinition("get_weather") { Title = "Weather", Icon = "Cloud" },
        (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("12 degrees")))]);

    [TestMethod]
    public async Task AChatHandedItsSessionShouldAnswerWithNoAiServiceRegistered()
    {
        var transport = new BitAiFakeTransport(
            new BitAiMessageStartedEvent(),
            new BitAiTextDeltaEvent("Here is **bold** and `code`.\n\n```csharp\nvar x = 1;\n```"),
            new BitAiMessageCompletedEvent());
        var session = new BitAiChatSession(transport, options: new BitAiChatSessionOptions { Pacing = null });

        var chat = RenderComponent<BitAiChat>(p => p.Add(c => c.Session, session));

        await chat.InvokeAsync(() => session.SubmitAsync("hello"));

        // The answer is drawn by the built-in renderers: no registry is registered or cascaded.
        chat.WaitForAssertion(() => Assert.IsNotNull(chat.Find(".bit-aimsl strong")));
        Assert.AreEqual("bold", chat.Find(".bit-aimsl strong").TextContent);
        StringAssert.Contains(chat.Find(".bit-aimsl").TextContent, "var x = 1;");

        await session.DisposeAsync();
    }

    [TestMethod]
    public async Task AChatHandedATransportShouldOwnASessionThatFollowsItsUiOptions()
    {
        var transport = new BitAiFakeTransport(
            new BitAiMessageStartedEvent(),
            new BitAiTextDeltaEvent("Hello **there**"),
            new BitAiMessageCompletedEvent());
        var strings = new LazyStrings();

        var chat = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Services, new BitAiServices { Transport = transport, Tools = WeatherTools() })
            .Add(c => c.UiOptions, BitAiUiOptions.Default with { Strings = strings, SmoothStreaming = false }));

        // The UI reads the options: the empty state is titled by their strings.
        StringAssert.Contains(chat.Find(".bit-aiems").TextContent, "Ask the lazy chat");

        // The session the chat created over the page's transport writes its errors in the same words, takes the text
        // unpaced, and offers the model the page's tools.
        var session = chat.Instance.ActiveSession;
        Assert.AreSame(strings, session.Strings);
        Assert.IsNull(session.Options.Pacing);

        await chat.InvokeAsync(() => session.SubmitAsync("hello"));

        chat.WaitForAssertion(() => Assert.AreEqual("there", chat.Find(".bit-aimsl strong").TextContent));
        Assert.HasCount(1, transport.Requests);
        Assert.IsTrue(transport.Requests[0].Tools.Any(t => t.Name == "get_weather"), "The page's tools were not offered to the model.");
    }

    [TestMethod]
    public void AChatHandedNeitherASessionNorATransportShouldSaySo()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RenderComponent<BitAiChat>());

        StringAssert.Contains(exception.Message, "Services with a Transport");
    }

    [TestMethod]
    public void ACascadedRendererRegistryShouldBeUsedWithNoAiServiceRegistered()
    {
        var renderers = new BitAiRendererRegistry();
        renderers.RegisterCodeLanguage("mermaid", _ => builder => builder.AddMarkupContent(0, "<div class=\"diagram\">drawn</div>"));

        var view = Context.Render<CascadingValue<BitAiRendererRegistry>>(p => p
            .Add(c => c.Value, renderers)
            .AddChildContent<BitAiMarkdownView>(c => c.Add(x => x.Document, BitAiMarkdownParser.Parse("```mermaid\ngraph TD\n```"))));

        Assert.AreEqual("drawn", view.Find(".diagram").TextContent);
    }

    [TestMethod]
    public void TheRenderersOfCascadedServicesShouldBeUsed()
    {
        var renderers = new BitAiRendererRegistry();
        renderers.RegisterCodeLanguage("mermaid", _ => builder => builder.AddMarkupContent(0, "<div class=\"diagram\">drawn</div>"));

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { Renderers = renderers })
            .AddChildContent<BitAiMarkdownView>(c => c.Add(x => x.Document, BitAiMarkdownParser.Parse("```mermaid\ngraph TD\n```"))));

        Assert.AreEqual("drawn", view.Find(".diagram").TextContent);
    }

    [TestMethod]
    public void AGenerativeUiViewShouldRenderThePlaceholderWithNoRegistry()
    {
        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo"}""")));

        StringAssert.Contains(view.Find(".bit-aiguv-unr").TextContent, "forecast_card");
    }

    [TestMethod]
    public void AGenerativeUiViewShouldUseTheRegistryOfCascadedServices()
    {
        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<ForecastCard>("forecast_card");

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { GenerativeUi = registry })
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, CompletedPart("forecast_card", """{"city":"Oslo"}"""))));

        Assert.AreEqual("Oslo", view.Find(".forecast").TextContent);
        Assert.IsEmpty(view.FindAll(".bit-aiguv-unr"));
    }

    [TestMethod]
    public void TheRegistryOfCascadedServicesShouldWinOverTheRegisteredOne()
    {
        Context.Services.AddBitBlazorUIAIServices(ai => ai.ConfigureGenerativeUi(r => r.Register<ForecastCard>("registered_card")));

        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<ForecastCard>("cascaded_card");

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { GenerativeUi = registry })
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, CompletedPart("registered_card", """{"city":"Oslo"}"""))));

        Assert.IsEmpty(view.FindAll(".forecast"));
        StringAssert.Contains(view.Find(".bit-aiguv-unr").TextContent, "registered_card");
    }

    [TestMethod]
    public void AGenerativeUiViewShouldLetGoOfServicesNoLongerCascaded()
    {
        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<ForecastCard>("forecast_card");
        var part = CompletedPart("forecast_card", """{"city":"Oslo"}""");

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { GenerativeUi = registry })
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, part)));

        Assert.AreEqual("Oslo", view.Find(".forecast").TextContent);

        view.Render(p => p
            .Add(c => c.Value, new BitAiServices())
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, part)));

        Assert.IsEmpty(view.FindAll(".forecast"));
        StringAssert.Contains(view.Find(".bit-aiguv-unr").TextContent, "forecast_card");
    }

    [TestMethod]
    public void AToolCallShouldBeNamedByTheToolsOfItsSession()
    {
        var session = new BitAiChatSession(new BitAiFakeTransport(), tools: WeatherTools());
        var call = new BitAiToolCallPart("c1", "get_weather");

        var card = RenderComponent<BitAiToolCallView>(p => p.Add(c => c.Call, call).Add(c => c.Session, session));

        Assert.AreEqual("Calling Weather", card.Find(".bit-aitcv-ttl").TextContent);
        Assert.IsTrue(card.Find("i.bit-aitcv-ico").ClassList.Contains("bit-icon--Cloud"));
    }

    [TestMethod]
    public void AToolCallShouldBeNamedByTheToolsOfCascadedServices()
    {
        var call = new BitAiToolCallPart("c1", "get_weather");

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { Tools = WeatherTools() })
            .AddChildContent<BitAiToolCallView>(c => c.Add(x => x.Call, call)));

        Assert.AreEqual("Calling Weather", view.Find(".bit-aitcv-ttl").TextContent);
    }

    [TestMethod]
    public void AReplyShouldNameItsModelByTheCatalogOfCascadedServices()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiTextPart("answer"));
        message.MergeMetadata(new BitAiGenerationMetadata { ModelId = "gpt-test" });

        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { ModelCatalog = new BitAiModelCatalog([new BitAiModel("gpt-test", "GPT Test")]) })
            .AddChildContent<BitAiMessageActions>(c => c
                .Add(x => x.Message, message)
                .Add(x => x.CascadedOptions, BitAiUiOptions.Default with { ShowModelName = true })));

        Assert.AreEqual("GPT Test", view.Find(".bit-aimac-mdl").TextContent);
    }

    [TestMethod]
    public void AModelSelectorShouldOfferTheCatalogOfCascadedServices()
    {
        var view = Context.Render<CascadingValue<BitAiServices>>(p => p
            .Add(c => c.Value, new BitAiServices { ModelCatalog = new BitAiModelCatalog([new BitAiModel("gpt-test", "GPT Test")]) })
            .AddChildContent<BitAiModelSelector>());

        StringAssert.Contains(view.Markup, "GPT Test");
    }
}
