namespace Boilerplate.WorkbenchTests.Infrastructure;

internal sealed class FixedHttpContextAccessor(HttpContext context) : IHttpContextAccessor
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
