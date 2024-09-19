﻿using System.Text.Json;

namespace Boilerplate.Client.Core.Components;

public partial class AppComponentBase : ComponentBase, IAsyncDisposable
{
    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [AutoInject] protected IJSRuntime JSRuntime = default!;

    [AutoInject] protected IStorageService StorageService = default!;

    [AutoInject] protected HttpClient HttpClient = default!;

    [AutoInject] protected JsonSerializerOptions JsonSerializerOptions = default!;

    /// <summary>
    /// <inheritdoc cref="IPrerenderStateService"/>
    /// </summary>
    [AutoInject] protected IPrerenderStateService PrerenderStateService = default!;

    /// <summary>
    /// <inheritdoc cref="IPubSubService"/>
    /// </summary>
    [AutoInject] protected IPubSubService PubSubService = default!;

    [AutoInject] protected IConfiguration Configuration = default!;

    [AutoInject] protected NavigationManager NavigationManager = default!;

    [AutoInject] protected IAuthTokenProvider AuthTokenProvider = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    [AutoInject] protected IExceptionHandler ExceptionHandler = default!;

    [AutoInject] protected AuthenticationManager AuthenticationManager = default!;


    private readonly CancellationTokenSource cts = new();
    protected CancellationToken CurrentCancellationToken => cts.Token;

    protected bool InPrerenderSession => JSRuntime.IsInitialized() is false;

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
            ExceptionHandler.Handle(exp);
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
            ExceptionHandler.Handle(exp);
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
                ExceptionHandler.Handle(exp);
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
}
