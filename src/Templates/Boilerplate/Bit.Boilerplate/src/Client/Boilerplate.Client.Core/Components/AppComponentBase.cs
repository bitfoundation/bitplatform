namespace Boilerplate.Client.Core.Components;

public partial class AppComponentBase : OwningComponentBase, IAsyncDisposable
{
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [AutoInject] private IServiceProvider serviceProvider = default!;

    protected IJSRuntime JSRuntime => field ??= serviceProvider.GetRequiredService<IJSRuntime>();

    protected IStorageService StorageService => field ??= serviceProvider.GetRequiredService<IStorageService>();

    [AutoInject] protected JsonSerializerOptions JsonSerializerOptions = default!;

    protected TimeProvider TimeProvider => field ??= serviceProvider.GetRequiredService<TimeProvider>();

    /// <summary>
    /// <inheritdoc cref="Infrastructure.Services.PubSubService"/>
    /// </summary>
    [AutoInject] protected PubSubService PubSubService = default!;

    protected IConfiguration Configuration => field ??= serviceProvider.GetRequiredService<IConfiguration>();

    [AutoInject] protected NavigationManager NavigationManager = default!;

    protected IAuthTokenProvider AuthTokenProvider => field ??= serviceProvider.GetRequiredService<IAuthTokenProvider>();

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    [AutoInject] protected ClientExceptionHandlerBase ExceptionHandler = default!;

    [AutoInject] protected AuthManager AuthManager = default!;

    [AutoInject] protected SnackBarService SnackBarService = default!;

    protected ITelemetryContext TelemetryContext => field ??= serviceProvider.GetRequiredService<ITelemetryContext>();

    /// <summary>
    /// <inheritdoc cref="Infrastructure.Services.TimeZoneService"/>
    /// </summary>
    [AutoInject] protected TimeZoneService TimeZoneService = default!;

    /// <summary>
    /// <inheritdoc cref="ISharedServiceCollectionExtensions.ConfigureAuthorizationCore"/>
    /// </summary>
    protected IAuthorizationService AuthorizationService => field ??= serviceProvider.GetRequiredService<IAuthorizationService>();

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
                throw new OperationCanceledException("Component already disposed.");
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


    private bool isSettingParameters;
    private object? renderedParametersKey;

    /// <summary>
    /// Opt-in, for the heavy components that stay on screen from the app's start: what this component renders from
    /// its parameters and cascading values. A parameter update that leaves it as it was at the last render doesn't
    /// re-render the component. That matters because Blazor can't tell whether an instance of a class changed, so it
    /// re-renders every component a class instance (UserDto, TenantDto, a list, a RenderFragment) is passed or cascaded
    /// to, each time its parent renders.
    /// Return a value tuple. Use enums, strings and primitives as they are. For a class, use the members the markup shows
    /// rather than the instance, since a page can change the instance in place and cascade it again (See ChangeEmailTab).
    /// Renders the component asks for itself (StateHasChanged, its own event handlers) are never skipped.
    /// Null, the default, keeps Blazor's behavior of rendering on every parameter update.
    /// </summary>
    protected virtual object? GetParametersRenderKey() => null;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (renderedParametersKey is null)
        {
            // Opted out, or the first update, whose render ShouldRender isn't asked about. That render is queued by the
            // time base.SetParametersAsync returns, so this is the key it renders with.
            var task = base.SetParametersAsync(parameters);
            renderedParametersKey = GetParametersRenderKey();
            return task;
        }

        // ComponentBase asks for the render a parameter update causes before SetParametersAsync returns, so whatever asks
        // for a render after that point is the component itself.
        isSettingParameters = true;
        try
        {
            return base.SetParametersAsync(parameters);
        }
        finally
        {
            isSettingParameters = false;
        }
    }

    protected override bool ShouldRender()
    {
        var key = GetParametersRenderKey();

        if (isSettingParameters)
        {
            // At most one render per update is skipped: if OnParamsSetAsync asked for one first, the update's own render still runs.
            isSettingParameters = false;

            if (key is not null && Equals(key, renderedParametersKey))
                return false;
        }

        renderedParametersKey = key;
        return true;
    }


    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Action WrapHandled(Action action,
        [CallerArgumentExpression("action")] string? argExpression = null,
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
                HandleException(exp, null, argExpression, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Action<T> WrapHandled<T>(Action<T> func,
        [CallerArgumentExpression("func")] string? argExpression = null,
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
                HandleException(exp, null, argExpression, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<Task> WrapHandled(Func<Task> func,
        [CallerArgumentExpression("func")] string? argExpression = null,
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
                HandleException(exp, null, argExpression, lineNumber, memberName, filePath);
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<Task<T>> WrapHandled<T>(Func<Task<T>> func,
        [CallerArgumentExpression("func")] string? argExpression = null,
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
                HandleException(exp, null, argExpression, lineNumber, memberName, filePath);
                return default;
            }
        };
    }

    /// <summary>
    /// Executes passed action that catches and handles all exceptions internally, preventing them from triggering the application's error boundary.
    /// </summary>
    public virtual Func<T, Task> WrapHandled<T>(Func<T, Task> func,
        [CallerArgumentExpression("func")] string? argExpression = null,
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
                HandleException(exp, null, argExpression, lineNumber, memberName, filePath);
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

        await currentCts.TryCancel();
    }

    private bool disposed;
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;

        using var currentCts = cts;
        cts = null;

        try
        {
            var cancellation = currentCts?.TryCancel() ?? Task.CompletedTask;

            await DisposeAsync(true);

            await cancellation;
        }
        finally
        {
            await DisposeAsyncCore(); // Would dispose OwiningComponentBase's ScopedServices
        }
    }

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        return ValueTask.CompletedTask;
    }

    private void HandleException(Exception exp,
        Dictionary<string, object?>? parameters = null,
        string? argExpression = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        parameters ??= [];

        if (AppPlatform.IsBlazorHybridOrBrowser is false)
        {
            parameters[nameof(InPrerenderSession)] = InPrerenderSession;
        }
        if (string.IsNullOrWhiteSpace(argExpression) is false)
        {
            parameters["Expression"] = argExpression;
        }
        parameters["ComponentType"] = GetType().FullName;

        ExceptionHandler.Handle(exp, ExceptionDisplayKind.Default, parameters, lineNumber, memberName, filePath);
    }
}
