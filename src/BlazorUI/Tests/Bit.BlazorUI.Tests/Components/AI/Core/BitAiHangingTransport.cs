using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// A transport that never finishes, so a test can stop it.
/// </summary>
internal sealed class BitAiHangingTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(
        BitAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();

        while (true)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            yield return new BitAiTextDeltaEvent("token ");
        }
    }
}
