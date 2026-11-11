namespace Boilerplate.WorkbenchTests.Infrastructure;

internal sealed class WorkbenchTestReviewer : IAsyncDisposable
{
    private readonly AsyncServiceScope scope;

    // The review document isn't thread-safe, so it runs on a dispatcher, as on a circuit.
    private readonly Dispatcher dispatcher;

    private WorkbenchTestReviewer(AsyncServiceScope scope, Dispatcher dispatcher, WorkbenchReviewSession session, string name)
    {
        this.scope = scope;
        this.dispatcher = dispatcher;
        Session = session;
        Name = name;
    }

    public WorkbenchReviewSession Session { get; }

    public string Name { get; }

    public static async Task<WorkbenchTestReviewer> Open(WorkbenchTestServer server, bool isLocal = true, string name = "Reviewer", bool isInvited = false)
    {
        var scope = server.Services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = CreateHttpContext(server.Address, isLocal, isInvited);

        var session = scope.ServiceProvider.GetRequiredService<WorkbenchReviewSession>();

        // Decided once, while the request above is the current one.
        _ = session.CanWrite;

        var dispatcher = Dispatcher.CreateDefault();

        await dispatcher.InvokeAsync(() => session.Open(name));

        return new(scope, dispatcher, session, name);
    }

    /// <summary>The request a circuit starts from: on this machine, or through a tunnel that names the visitor, with or without an invite's cookie.</summary>
    public static HttpContext CreateHttpContext(Uri address, bool isLocal, bool isInvited = false)
    {
        var context = new DefaultHttpContext();

        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Scheme = address.Scheme;
        context.Request.Host = new HostString(address.Host, address.Port);

        if (isLocal is false)
        {
            context.Request.Headers["Cf-Connecting-Ip"] = "203.0.113.7";
        }

        if (isInvited)
        {
            context.Request.Headers.Cookie = $"{WorkbenchReviewers.CookieName}={WorkbenchReviewers.Key}";
        }

        return context;
    }

    /// <summary>Adds a comment on a page and waits until the server's copy of the review has it.</summary>
    public async Task<string> Comment(WorkbenchTestServer server, string pageKey, string text, string query = "", string? component = "BitButton", string? state = null)
    {
        var id = await dispatcher.InvokeAsync(() => Session.AddThread(pageKey, query, new("main button", component, "Save", 0.5, 0.5), Name, text, state))
            ?? throw new InvalidOperationException("The review session refused the comment.");

        await WaitFor(() => server.Agent.GetThreads(CancellationToken.None), threads => threads.Any(t => t.Id == id));

        return id;
    }

    /// <summary>Adds a comment on a page without waiting for the server's copy, which a visitor's never reaches.</summary>
    public Task<string?> AddComment(string pageKey, string text) => dispatcher.InvokeAsync(() => Session.AddThread(pageKey, "", new("body", null, null, 0, 0), Name, text, null));

    /// <summary>Changes the review on the session's dispatcher.</summary>
    public Task Change(Action<WorkbenchReviewSession> change) => dispatcher.InvokeAsync(() => change(Session));

    /// <summary>Reads until the condition holds, or fails after the timeout.</summary>
    public static async Task<T> WaitFor<T>(Func<Task<T>> read, Func<T, bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));

        while (true)
        {
            var value = await read();

            if (condition(value))
                return value;

            if (DateTimeOffset.UtcNow > deadline)
                throw new TimeoutException("The review didn't reach the expected state in time.");

            await Task.Delay(50);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await dispatcher.InvokeAsync(() => scope.DisposeAsync().AsTask());
    }
}
