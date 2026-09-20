using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A configured rewriter, created by <see cref="Rewriter.Create"/> - it transforms text it is given
/// rather than producing new text.
/// </summary>
/// <remarks>Dispose it when you are done - see <see cref="AiSession"/>.</remarks>
public sealed class RewriterSession : AiSession
{
    internal RewriterSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Rewrites a piece of text the way the session was configured to.
    /// </summary>
    /// <param name="input">The text to rewrite.</param>
    /// <param name="context">
    /// Extra context for this input alone, on top of <see cref="RewriterOptions.SharedContext"/> -
    /// "this is going to a customer" changes the result more than any option does.
    /// </param>
    /// <returns>The rewritten text, or null once the session has been disposed.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public ValueTask<string?> Rewrite(string input, string? context = null)
        => RunCore(input, new AiRunJsOptions { Context = context });

    /// <summary>
    /// Rewrites a piece of text, reporting it as it is generated.
    /// </summary>
    /// <param name="input">The text to rewrite.</param>
    /// <param name="onChunk">Called with each new piece of text - the delta, so append it.</param>
    /// <param name="context">Extra context for this input alone, on top of <see cref="RewriterOptions.SharedContext"/>.</param>
    /// <returns>The whole text, once the stream ends.</returns>
    /// <exception cref="InvalidOperationException">The stream failed, or the session is gone.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiRunJsOptions))]
    public Task<string> RewriteStreaming(string input, Action<string>? onChunk = null, string? context = null)
        => RunStreamingCore(input, new AiRunJsOptions { Context = context }, onChunk);
}
