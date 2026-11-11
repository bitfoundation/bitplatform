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
/// assembly there. Everything the registration gives them is handed over on the page instead: the session, the options,
/// the renderers and the generative UI registry.
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

    private static BitAiGenerativeUiPart CompletedPart(string name, string props)
    {
        using var document = JsonDocument.Parse(props);
        var part = new BitAiGenerativeUiPart(name);
        part.SetProps(document.RootElement.Clone());
        part.Complete();
        return part;
    }

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
    public void AChatShouldTakeItsUiOptionsWithNoAiServiceRegistered()
    {
        var session = new BitAiChatSession(new BitAiFakeTransport());

        var chat = RenderComponent<BitAiChat>(p => p
            .Add(c => c.Session, session)
            .Add(c => c.EmptyTitle, "Ask the lazy chat"));

        StringAssert.Contains(chat.Find(".bit-aiems").TextContent, "Ask the lazy chat");
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
    public void AGenerativeUiViewShouldRenderThePlaceholderWithNoRegistry()
    {
        var view = RenderComponent<BitAiGenerativeUiView>(p => p.Add(c => c.Part, CompletedPart("forecast_card", """{"city":"Oslo"}""")));

        StringAssert.Contains(view.Find(".bit-aiguv-unr").TextContent, "forecast_card");
    }

    [TestMethod]
    public void AGenerativeUiViewShouldUseACascadedRegistry()
    {
        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<ForecastCard>("forecast_card");

        var view = Context.Render<CascadingValue<IBitAiGenerativeUiRegistry>>(p => p
            .Add(c => c.Value, registry)
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, CompletedPart("forecast_card", """{"city":"Oslo"}"""))));

        Assert.AreEqual("Oslo", view.Find(".forecast").TextContent);
        Assert.IsEmpty(view.FindAll(".bit-aiguv-unr"));
    }

    [TestMethod]
    public void ACascadedGenerativeUiRegistryShouldWinOverTheRegisteredOne()
    {
        Context.Services.AddBitBlazorUIAIServices(ai => ai.ConfigureGenerativeUi(r => r.Register<ForecastCard>("registered_card")));

        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<ForecastCard>("cascaded_card");

        var view = Context.Render<CascadingValue<IBitAiGenerativeUiRegistry>>(p => p
            .Add(c => c.Value, registry)
            .AddChildContent<BitAiGenerativeUiView>(c => c.Add(x => x.Part, CompletedPart("registered_card", """{"city":"Oslo"}"""))));

        Assert.IsEmpty(view.FindAll(".forecast"));
        StringAssert.Contains(view.Find(".bit-aiguv-unr").TextContent, "registered_card");
    }
}
