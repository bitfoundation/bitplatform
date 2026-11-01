namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>The same request for every caller, whatever flow it runs in: one from this machine, or one through a tunnel.</summary>
public sealed class FixedHttpContextAccessor(HttpContext context) : IHttpContextAccessor
{
    public FixedHttpContextAccessor(bool isLocal) : this(WorkbenchTestReviewer.CreateHttpContext(new("http://localhost/"), isLocal))
    {
    }

    public HttpContext? HttpContext
    {
        get => context;
        set { }
    }
}
