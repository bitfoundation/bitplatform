using System.Runtime.CompilerServices;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The model behind the AI section's chats. It never writes on its own, and answers whatever is sent with the note it was
/// given, since the page shows the components and calls no model.
/// </summary>
public sealed class AiSampleTransport(string answer) : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();

        // A short pause, the way a model takes a moment before its first words.
        await Task.Delay(400, cancellationToken);

        yield return new BitAiTextDeltaEvent(answer);
        yield return new BitAiMessageCompletedEvent(BitAiFinishReason.Stop);
    }
}
