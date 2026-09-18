using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A configured proofreader, created by <see cref="Proofreader.Create"/>.
/// </summary>
/// <remarks>Dispose it when you are done - see <see cref="AiSession"/>.</remarks>
public sealed class ProofreaderSession : AiSession
{
    internal ProofreaderSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Corrects a piece of text and reports each change positioned in the original, so a UI can
    /// highlight what was wrong instead of silently replacing it.
    /// </summary>
    /// <returns>The result, or null once the session has been disposed.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProofreadResult))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProofreadCorrection))]
    public ValueTask<ProofreadResult?> Proofread(string input)
        => Js.Invoke<ProofreadResult?>("BitButil.ai.proofread", Id, input);
}
