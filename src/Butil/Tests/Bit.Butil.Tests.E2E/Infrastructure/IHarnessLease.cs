using Microsoft.Playwright;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// A page one test has to itself, on the host the run drives, for as long as the lease is held. Disposing it
/// hands the page back: a web browser context is closed, a hybrid app window returns to its pool.
/// </summary>
public interface IHarnessLease : IAsyncDisposable
{
    IPage Page { get; }

    /// <summary>The origin the harness pages are served from, without a trailing slash.</summary>
    string BaseUrl { get; }
}
