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

    [AutoInject] protected HttpClient HttpClient = default!;

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


    private CancellationTokenSource cts = new();
    protected CancellationToken CurrentCancellationToken => cts.Token;

    protected bool InPrerenderSession => AppPlatform.IsBlazorHybrid is false && JSRuntime.IsInitialized() is false;

    protected sealed override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected sealed override async Task OnInitializedAsync()
    {
        try
        {
            await OnInitAsync();
            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            HandleException(exp);
        }
    }

    /// <summary>
    /// Replacement for <see cref="OnInitializedAsync"/> which catches all possible exceptions in order to prevent app crash.
    /// </summary>
    protected virtual Task OnInitAsync()
    {
        return Task.CompletedTask;
    }


    protected sealed override async Task OnParametersSetAsync()
    {
        try
        {
            await OnParamsSetAsync();
            await base.OnParametersSetAsync();
        }
        catch (Exception exp)
        {
            HandleException(exp);
        }
    }

    /// <summary>
    /// Replacement for <see cref="OnParametersSetAsync"/> which catches all possible exceptions in order to prevent app crash.
    /// </summary>
    protected virtual Task OnParamsSetAsync()
    {
        return Task.CompletedTask;
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
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

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// Method invoked after first time the component has been rendered.
    /// </summary>
    protected virtual Task OnAfterFirstRenderAsync()
    {
        return Task.CompletedTask;
    }


    /// <summary>
    /// Executes passed action while catching all possible exceptions to prevent app crash.
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
    /// Executes passed action while catching all possible exceptions to prevent app crash.
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
    /// Executes passed function while catching all possible exceptions to prevent app crash.
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
    /// Executes passed function while catching all possible exceptions to prevent app crash.
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
    /// Cancells running codes inside current component.
    /// </summary>
    protected void Abort()
    {
        cts.Cancel();
        cts.Dispose();
        cts = new();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await PrerenderStateService.DisposeAsync();
            cts.Cancel();
            cts.Dispose();
        }
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
