using Microsoft.Extensions.Http;

namespace Bit.BevTools;

/// <summary>
/// Adds the loader's hooks to every <c>IHttpClientFactory</c> pipeline: one outermost, where the issuing session can
/// still be told, and one innermost, so each retry attempt is recorded. Both pass requests through until BevTools is
/// attached.
/// </summary>
internal sealed class BevToolsHttpHookFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
    {
        next(builder);
        builder.AdditionalHandlers.Insert(0, new SessionHook());
        builder.AdditionalHandlers.Add(new RecordHook());
    };

    private sealed class SessionHook : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            BevToolsHooks.Http?.CaptureSession(request);
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>Internal so a session attached at runtime can tell a pipeline that already records.</summary>
    internal sealed class RecordHook : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _next;

        public RecordHook()
        {
            _next = base.SendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            BevToolsHooks.Http is { } recorder
                ? recorder.SendAsync(request, _next, attachedSessionsOnly: true, cancellationToken)
                : base.SendAsync(request, cancellationToken);
    }
}
