using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The three calls every built-in AI service makes in the same way - support probe, availability
/// probe and session creation - kept in one place so the seven services differ only in the API name
/// they pass and the session type they hand back.
/// </summary>
internal static class AiApi
{
    internal const string LanguageModel = "languageModel";
    internal const string Summarizer = "summarizer";
    internal const string Translator = "translator";
    internal const string LanguageDetector = "languageDetector";
    internal const string Writer = "writer";
    internal const string Rewriter = "rewriter";
    internal const string Proofreader = "proofreader";

    internal static ValueTask<bool> IsSupported(IJSRuntime js, string api)
        => js.Invoke<bool>("BitButil.ai.isSupported", api);

    internal static async ValueTask<AiAvailability> Availability(IJSRuntime js, string api, object? options)
    {
        var raw = await js.Invoke<string>("BitButil.ai.availability", api, options);

        return raw switch
        {
            "available" => AiAvailability.Available,
            "downloadable" => AiAvailability.Downloadable,
            "downloading" => AiAvailability.Downloading,
            _ => AiAvailability.Unavailable,
        };
    }

    /// <summary>
    /// Creates a session and returns the id the JS side keeps it under, or null when the runtime
    /// refused - no such API, an option set it can't serve, or a declined model download.
    /// </summary>
    internal static async ValueTask<Guid?> Create(
        IJSRuntime js,
        AiInterop interop,
        string api,
        object? options,
        Action<double>? onDownloadProgress)
    {
        var sessionId = Guid.NewGuid();
        var progressId = interop.BeginProgress(onDownloadProgress);

        try
        {
            var outcome = await js.Invoke<string>("BitButil.ai.create",
                api, sessionId, options, interop.DotNetRef, progressId, AiInterop.ProgressMethodName);

            return outcome == "created" ? sessionId : null;
        }
        finally
        {
            // The monitor only reports during creation, so the handler is dropped as soon as the
            // call returns either way - otherwise every session created would leak one.
            interop.EndProgress(progressId);
        }
    }
}
