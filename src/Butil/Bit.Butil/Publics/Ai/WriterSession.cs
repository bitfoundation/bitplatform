using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A configured writer, created by <see cref="Writer.Create"/> - it produces new text from a short
/// prompt.
/// </summary>
/// <remarks>Dispose it when you are done - see <see cref="AiSession"/>.</remarks>
public sealed class WriterSession : AiSession
{
    internal WriterSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Writes text from a prompt.
    /// </summary>
    /// <param name="input">What to write - "a reply declining the meeting, politely".</param>
    /// <param name="context">
    /// Extra context for this prompt alone, on top of <see cref="WriterOptions.SharedContext"/>.
    /// </param>
    /// <returns>The text, or null once the session has been disposed.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public ValueTask<string?> Write(string input, string? context = null)
        => RunCore(input, new AiRunJsOptions { Context = context });

    /// <summary>
    /// Writes text from a prompt, reporting it as it is generated - the shape a "draft this for me"
    /// button wants.
    /// </summary>
    /// <param name="input">What to write.</param>
    /// <param name="onChunk">Called with each new piece of text - the delta, so append it.</param>
    /// <param name="context">Extra context for this prompt alone, on top of <see cref="WriterOptions.SharedContext"/>.</param>
    /// <returns>The whole text, once the stream ends.</returns>
    /// <exception cref="InvalidOperationException">The stream failed, or the session is gone.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public Task<string> WriteStreaming(string input, Action<string>? onChunk = null, string? context = null)
        => RunStreamingCore(input, new AiRunJsOptions { Context = context }, onChunk);
}
