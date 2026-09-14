using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A conversation with the on-device language model, created by <see cref="LanguageModel.Create"/>.
/// </summary>
/// <remarks>
/// The session is stateful: every <see cref="Prompt"/> sees the turns before it, and each turn
/// spends part of the session's quota (see <see cref="AiSession.GetUsage"/>). Dispose it when the
/// conversation is over - see <see cref="AiSession"/> for why that matters.
/// </remarks>
public sealed class LanguageModelSession : AiSession
{
    internal LanguageModelSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Sends a turn and waits for the whole answer.
    /// </summary>
    /// <returns>The model's reply, or null once the session has been disposed.</returns>
    /// <remarks>
    /// A long answer keeps the user waiting with nothing on screen - prefer
    /// <see cref="PromptStreaming"/> for anything conversational.
    /// </remarks>
    public ValueTask<string?> Prompt(string input) => RunCore(input, null);

    /// <summary>
    /// Sends a turn and reports the answer as it is generated.
    /// </summary>
    /// <param name="input">The turn to send.</param>
    /// <param name="onChunk">
    /// Called with each new piece of text - the delta, not the text so far, so append it. Called on
    /// the interop dispatch, so a Blazor component has to <c>StateHasChanged</c> itself.
    /// </param>
    /// <returns>The whole answer, once the stream ends.</returns>
    /// <exception cref="InvalidOperationException">The stream failed, or the session is gone.</exception>
    public Task<string> PromptStreaming(string input, Action<string>? onChunk = null)
        => RunStreamingCore(input, null, onChunk);

    /// <summary>
    /// Adds turns to the conversation without asking for a reply - context the model should have,
    /// but that needs no answer.
    /// </summary>
    /// <returns>False when the runtime doesn't implement <c>append</c>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiPrompt))]
    public ValueTask<bool> Append(params AiPrompt[] prompts)
        => Js.Invoke<bool>("BitButil.ai.append", Id, prompts);

    /// <summary>
    /// How much quota a turn would cost, without sending it - the check to make before a long input
    /// silently pushes the start of the conversation out of the window.
    /// </summary>
    /// <returns>The token count, or -1 when the runtime can't measure it.</returns>
    public ValueTask<double> MeasureInputUsage(string input)
        => Js.Invoke<double>("BitButil.ai.measureInputUsage", Id, input, null);

    /// <summary>
    /// Forks the conversation: a new session with the same history, which then diverges from this one.
    /// </summary>
    /// <returns>The fork, or null when the runtime doesn't implement <c>clone</c>.</returns>
    /// <remarks>
    /// Cheaper than replaying the history into a fresh session, and the usual way to offer
    /// "regenerate this answer" without losing the original.
    /// </remarks>
    public async ValueTask<LanguageModelSession?> Clone()
    {
        var id = Guid.NewGuid();
        var cloned = await Js.Invoke<bool>("BitButil.ai.clone", Id, id);
        return cloned ? new LanguageModelSession(Js, Interop, id) : null;
    }
}
