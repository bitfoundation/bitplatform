using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A configured summarizer, created by <see cref="Summarizer.Create"/>. Reuse it across documents -
/// the configuration and the loaded model are what a session holds.
/// </summary>
/// <remarks>Dispose it when you are done - see <see cref="AiSession"/>.</remarks>
public sealed class SummarizerSession : AiSession
{
    internal SummarizerSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Summarizes a piece of text.
    /// </summary>
    /// <param name="input">The text to summarize.</param>
    /// <param name="context">
    /// Extra context for this input alone, on top of <see cref="SummarizerOptions.SharedContext"/> -
    /// what this particular document is.
    /// </param>
    /// <returns>The summary, or null once the session has been disposed.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public ValueTask<string?> Summarize(string input, string? context = null)
        => RunCore(input, new AiRunJsOptions { Context = context });

    /// <summary>
    /// Summarizes a piece of text, reporting the summary as it is generated.
    /// </summary>
    /// <param name="input">The text to summarize.</param>
    /// <param name="onChunk">Called with each new piece of text - the delta, so append it.</param>
    /// <param name="context">Extra context for this input alone, on top of <see cref="SummarizerOptions.SharedContext"/>.</param>
    /// <returns>The whole summary, once the stream ends.</returns>
    /// <exception cref="InvalidOperationException">The stream failed, or the session is gone.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public Task<string> SummarizeStreaming(string input, Action<string>? onChunk = null, string? context = null)
        => RunStreamingCore(input, new AiRunJsOptions { Context = context }, onChunk);
}
