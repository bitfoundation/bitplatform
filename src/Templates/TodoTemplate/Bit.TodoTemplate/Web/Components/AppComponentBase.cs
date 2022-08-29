namespace TodoTemplate.App.Components;

public partial class AppComponentBase : ComponentBase
{
    [AutoInject] protected IExceptionHandler ExceptionHandler { get; set; } = default!;

    [AutoInject] protected IStateService StateService = default!;

    [AutoInject] protected AppAuthenticationStateProvider AuthenticationStateProvider = default!;

    [AutoInject] protected HttpClient HttpClient = default!;

    [AutoInject] protected IAuthTokenProvider AuthTokenProvider = default!;

    [AutoInject] protected IConfiguration Configuration = default!;

    [AutoInject] protected NavigationManager NavigationManager = default!;

    [AutoInject] protected IAuthenticationService AuthenticationService = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    [AutoInject] protected IJSRuntime JSRuntime { get; set; } = default!;

    protected async sealed override Task OnInitializedAsync()
    {
        try
        {
            await OnInitAsync();
            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    protected async sealed override Task OnParametersSetAsync()
    {
        try
        {
            await OnParamsSetAsync();
            await base.OnParametersSetAsync();
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await OnAfterFirstRenderAsync();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
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
    /// Method invoked after first time the component has been rendered.
    /// </summary>
    protected virtual Task OnAfterFirstRenderAsync()
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
                ExceptionHandler.Handle(exp);
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
                ExceptionHandler.Handle(exp);
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
                ExceptionHandler.Handle(exp);
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
                ExceptionHandler.Handle(exp);
            }
        };
    }
}
