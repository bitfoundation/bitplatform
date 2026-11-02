using System.Runtime.CompilerServices;

namespace Boilerplate.LandingPage.Client.Components.Chat;

internal sealed class LandingAiTransport(ILandingChatClient chatClient, string site) : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();

        LandingChatTurn[] turns = [.. request.Messages
            .Where(m => m.Role == BitAiRole.User || m.Role == BitAiRole.Assistant)
            .Select(m => new LandingChatTurn(m.Role == BitAiRole.User, m.GetText()))
            .Where(t => string.IsNullOrWhiteSpace(t.Text) is false)];

        await using var deltas = chatClient.AskAsync(new(site, CultureInfo.CurrentUICulture.Name, turns), cancellationToken).GetAsyncEnumerator(cancellationToken);

        LandingChatException? failure = null;

        while (failure is null)
        {
            try
            {
                if (await deltas.MoveNextAsync() is false)
                    break;
            }
            catch (LandingChatException exp)
            {
                failure = exp;
                break;
            }

            yield return new BitAiTextDeltaEvent(deltas.Current);
        }

        if (failure is not null)
        {
            yield return new BitAiErrorEvent(new BitAiError(failure.Message) { IsRetryable = failure.Retryable });
            yield break;
        }

        yield return new BitAiMessageCompletedEvent();
    }
}
