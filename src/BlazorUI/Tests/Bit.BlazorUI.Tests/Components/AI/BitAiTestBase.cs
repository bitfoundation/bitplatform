using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI;

/// <summary>
/// A rendering context with the services of Bit.BlazorUI.AI registered, so the tests of its components read as usage
/// rather than as setup.
/// </summary>
public abstract class BitAiTestBase : BunitTestContext
{
    [TestInitialize]
    public void AddAiServices()
    {
        Context.Services.AddBitBlazorUIAIServices();
    }

    /// <summary>
    /// Has the renderer say it is an interactive one, the way a circuit renders: the transcript draws a bounded tail
    /// instead of virtualizing while it is prerendered. Called once every service a test adds is in, since setting it
    /// builds the renderer, which reads the services. Without it the components ask the JS runtime, which answers
    /// interactive as well (see <see cref="BitComponentBase"/>), so a test calls it only to say so outright.
    /// </summary>
    protected void Interactive()
    {
        Context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    /// <summary>The renderer registry, for tests that register a custom renderer.</summary>
    protected BitAiRendererRegistry Renderers => Services.GetRequiredService<BitAiRendererRegistry>();

    /// <summary>The generative UI registry, for tests that register a component.</summary>
    protected BitAiGenerativeUiRegistry Generative => Services.GetRequiredService<BitAiGenerativeUiRegistry>();

    /// <summary>Builds an assistant message from a sequence of parts.</summary>
    /// <param name="parts">The parts to add.</param>
    /// <returns>The message.</returns>
    protected static BitAiMessage Assistant(params BitAiMessagePart[] parts)
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        foreach (var part in parts) message.AddPart(part);
        return message;
    }
}
