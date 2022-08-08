namespace TodoTemplate.App.Components;

public partial class AppComponentBase : ComponentBase
{
    [AutoInject] protected IExceptionHandler _exceptionHandler { get; set; } = default!;

    [AutoInject] protected IStateService _stateService { get; set; } = default!;

    [AutoInject] protected AppAuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;

    [AutoInject] protected HttpClient _httpClient { get; set; } = default!;

    [AutoInject] protected IAuthTokenProvider _authTokenProvider { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] protected IConfiguration _configuration { get; set; } = default!;
#endif

    [AutoInject] protected NavigationManager _navigationManager { get; set; } = default!;

    [AutoInject] protected IAuthenticationService _authenticationService { get; set; } = default!;

    protected async sealed override Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();
            await OnInitAsync();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    protected async sealed override Task OnParametersSetAsync()
    {
        try
        {
            await base.OnParametersSetAsync();
            await OnParamsSetAsync();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    /// <summary>
    /// Replacement for <see cref="OnInitializedAsync"/> which catches all possible exceptions in order to prevent app crash.
    /// </summary>
    protected virtual Task OnInitAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replacement for <see cref="OnParametersSetAsync"/> which catches all possible exceptions in order to prevent app crash.
    /// </summary>
    protected virtual Task OnParamsSetAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes passed action while catching all possible exceptions to prevent app crash.
    /// </summary>
    public virtual Action WrapHandled(Action action)
    {
        return () =>
        {
            try
            {
                action();
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        };
    }

    /// <summary>
    /// Executes passed action while catching all possible exceptions to prevent app crash.
    /// </summary>
    public virtual Action<T> WrapHandled<T>(Action<T> func)
    {
        return (e) =>
        {
            try
            {
                func(e);
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        };
    }

    /// <summary>
    /// Executes passed function while catching all possible exceptions to prevent app crash.
    /// </summary>
    public virtual Func<Task> WrapHandled(Func<Task> func)
    {
        return async () =>
        {
            try
            {
                await func();
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        };
    }

    /// <summary>
    /// Executes passed function while catching all possible exceptions to prevent app crash.
    /// </summary>
    public virtual Func<T, Task> WrapHandled<T>(Func<T, Task> func)
    {
        return async (e) =>
        {
            try
            {
                await func(e);
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        };
    }
}
