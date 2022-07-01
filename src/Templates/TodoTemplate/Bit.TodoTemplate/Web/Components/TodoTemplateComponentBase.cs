namespace TodoTemplate.App.Components;

public class TodoTemplateComponentBase : ComponentBase
{
    [AutoInject] IExceptionHandler exceptionHandler = default!;

    protected async sealed override Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();
            await OnInitAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
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
            exceptionHandler.Handle(exp);
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
                exceptionHandler.Handle(exp);
            }
        };
    }

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
                exceptionHandler.Handle(exp);
            }
        };
    }

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
                exceptionHandler.Handle(exp);
            }
        };
    }

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
                exceptionHandler.Handle(exp);
            }
        };
    }
}
