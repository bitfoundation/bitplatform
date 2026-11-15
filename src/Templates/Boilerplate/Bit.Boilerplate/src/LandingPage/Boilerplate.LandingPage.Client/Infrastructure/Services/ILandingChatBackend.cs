namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

/// <summary>
/// Answers the chat right where its components run. The server registers it for Blazor Server, where a WebSocket back to
/// the server itself would only be a detour; WebAssembly leaves it out and opens a WebSocket per question instead (See
/// LandingAiTransport). It speaks the wire format of Bit.BlazorUI.AI as strings, exactly what the socket carries, so this
/// interface - read along with every other type of the assembly at startup - names no type of the lazily loaded
/// Bit.BlazorUI.AI assemblies.
/// </summary>
public interface ILandingChatBackend
{
    /// <param name="request">The question and the conversation before it, as BitAiWireSerializer.SerializeRequest writes them.</param>
    /// <returns>The events of the answer, each as BitAiWireSerializer.Serialize writes it.</returns>
    IAsyncEnumerable<string> StreamAsync(string request, CancellationToken cancellationToken);
}

/// <summary>What a question carries besides the conversation, in its BitAiRequest.Context.</summary>
public static class LandingChatContext
{
    /// <summary>The key of the sample the question is about (See <see cref="LandingSite.Key"/>), or <see cref="Gallery"/>.</summary>
    public const string Site = "site";

    /// <summary>The visitor's language, which the answer is written in.</summary>
    public const string Culture = "culture";

    /// <summary>The site of the gallery's own chat, which knows every sample rather than one.</summary>
    public const string Gallery = "gallery";

    /// <summary>The gallery chat's tool that shows the samples it recommends as cards the visitor can open.</summary>
    public const string ShowSamplesTool = "ShowSamples";

    /// <summary>
    /// How many of the conversation's newest messages go with a question, and the model reads: the conversation goes on as
    /// long as the visitor likes, while what one question sends stays bounded.
    /// </summary>
    public const int MaxTurns = 12;
}
