using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Components;

public partial class AppComponentBase : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    [CascadingParameter(Name = Parameters.IsOnline)] protected bool? IsOnline { get; set; }
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [AutoInject] protected IJSRuntime JSRuntime = default!;

    [AutoInject] protected IStorageService StorageService = default!;

    [AutoInject] protected JsonSerializerOptions JsonSerializerOptions = default!;

    /// <summary>
    /// <inheritdoc cref="IPrerenderStateService"/>
    /// </summary>
    [AutoInject] protected IPrerenderStateService PrerenderStateService = default!;

    /// <summary>
    /// <inheritdoc cref="Services.PubSubService"/>
    /// </summary>
    [AutoInject] protected PubSubService PubSubService = default!;

    [AutoInject] protected IConfiguration Configuration = default!;

    [AutoInject] protected NavigationManager NavigationManager = default!;

    [AutoInject] protected IAuthTokenProvider AuthTokenProvider = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    [AutoInject] protected IExceptionHandler ExceptionHandler = default!;

    [AutoInject] protected AuthManager AuthManager = default!;

    [AutoInject] protected SnackBarService SnackBarService = default!;

    [AutoInject] protected ITelemetryContext TelemetryContext = default!;

    /// <summary>
    /// <inheritdoc cref="ISharedServiceCollectionExtensions.ConfigureAuthorizationCore"/>
    /// </summary>
    [AutoInject] protected IAuthorizationService AuthorizationService = default!;

    /// <summary>
    /// <inheritdoc cref="AbsoluteServerAddressProvider" />
    /// </summary>
    [AutoInject] protected AbsoluteServerAddressProvider AbsoluteServerAddress { get; set; } = default!;


    private CancellationTokenSource? cts = new();
    protected CancellationToken CurrentCancellationToken
    {
        get
        {
            if (cts == null)
                throw new OperationCanceledException(); // Component already disposed.
            cts.Token.ThrowIfCancellationRequested();
            return cts.Token;
        }
    }

    protected bool InPrerenderSession => AppPlatform.IsBlazorHybrid is false && RendererInfo.IsInteractive is false;

    protected sealed override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected sealed override async Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();
            await OnInitAsync();
        }
        catch (Exception exp)
        {
            HandleException(exp);
        }
    }

    /// <summary>
    /// A safer alternative to `<see cref="OnInitializedAsync"/>` that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    protected virtual Task OnInitAsync()
    {
        return Task.CompletedTask;
    }


    protected sealed override async Task OnParametersSetAsync()
    {
        try
        {
            await base.OnParametersSetAsync();
            await OnParamsSetAsync();
        }
        catch (Exception exp)
        {
            HandleException(exp);
        }
    }

    /// <summary>
    /// A safer alternative to `<see cref="OnParametersSetAsync"/>` that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    protected virtual Task OnParamsSetAsync()
    {
        return Task.CompletedTask;
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                await OnAfterFirstRenderAsync();
            }
            catch (Exception exp)
            {
                HandleException(exp);
            }
        }
    }

    /// <summary>
    /// This method is executed only during the initial render of the component.
    /// It ensures that all potential exceptions are handled gracefully, preventing them from triggering the application's error boundary.
    /// </summary>
    protected virtual Task OnAfterFirstRenderAsync()
    {
        return Task.CompletedTask;
    }


    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Action WrapHandled(Action action,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return () =>
        {
            try
            {
                action();
            }
            catch (Exception exp)
            {
                HandleException(exp, null, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Action<T> WrapHandled<T>(Action<T> func,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return (e) =>
        {
            try
            {
                func(e);
            }
            catch (Exception exp)
            {
                HandleException(exp, null, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<Task> WrapHandled(Func<Task> func,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return async () =>
        {
            try
            {
                await func();
            }
            catch (Exception exp)
            {
                HandleException(exp, null, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<Task<T>> WrapHandled<T>(Func<Task<T>> func,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return async () =>
        {
            try
            {
                return await func();
            }
            catch (Exception exp)
            {
                HandleException(exp, null, lineNumber, memberName, filePath);
                return default;
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<T, Task> WrapHandled<T>(Func<T, Task> func,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        return async (e) =>
        {
            try
            {
                await func(e);
            }
            catch (Exception exp)
            {
                HandleException(exp, null, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Terminates any ongoing operations within the current component.
    /// </summary>
    protected async Task Abort()
    {
        if (cts == null)
            return; // Component already disposed.

        using var currentCts = cts;
        cts = new();

        await currentCts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (cts != null)
        {
            using var currentCts = cts;
            cts = null;
            await currentCts.CancelAsync();
        }

        await PrerenderStateService.DisposeAsync();

        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        return ValueTask.CompletedTask;
    }

    private void HandleException(Exception exp,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        parameters ??= [];

        if (AppPlatform.IsBlazorHybridOrBrowser is false)
        {
            parameters[nameof(InPrerenderSession)] = InPrerenderSession;
        }
        parameters["ComponentType"] = GetType().FullName;

        ExceptionHandler.Handle(exp, ExceptionDisplayKind.Default, parameters, lineNumber, memberName, filePath);
    }
}
