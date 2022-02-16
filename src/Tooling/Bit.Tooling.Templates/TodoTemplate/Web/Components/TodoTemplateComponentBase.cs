using Microsoft.AspNetCore.Components.Web;

namespace TodoTemplate.App.Components;

public class TodoTemplateComponentBase : ComponentBase
{
    [Inject] IExceptionHandler ExceptionHandler { get; set; } = default!;

    protected async sealed override Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();
            await OnInitAsync();
        }
        catch (Exception exp)
        {
            ExceptionHandler.OnExceptionReceived(exp);
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
            ExceptionHandler.OnExceptionReceived(exp);
        }
    }

    protected virtual Task OnInitAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnParamsSetAsync()
    {
        return Task.CompletedTask;
    }

    public virtual T? Evaluate<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (Exception exp)
        {
            ExceptionHandler.OnExceptionReceived(exp);
            return default;
        }
    }

    public virtual Func<Task> Invoke(Func<Task> func)
    {
        return async () =>
        {
            try
            {
                await func();
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        };
    }


    public virtual Action Invoke(Action action)
    {
        return () =>
        {
            try
            {
                action();
                Invoke(StateHasChanged);
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        };
    }

    public virtual Func<EventArgs, Task> Invoke(Func<EventArgs, Task> func)
    {
        return async (e) =>
        {
            try
            {
                await func(e);
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        };
    }

    public virtual Func<MouseEventArgs, Task> Invoke(Func<MouseEventArgs, Task> func)
    {
        return async (e) =>
        {
            try
            {
                await func(e);
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        };
    }
}
