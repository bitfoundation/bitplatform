using Bit.ViewModel;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.View
{
    public class ComponentBase<TViewModel> : ComponentBase, IAsyncDisposable
             where TViewModel : ViewModelBase
    {
        private TViewModel _ViewModel = default!;

        [Inject]
        public virtual TViewModel ViewModel
        {
            get => _ViewModel;
            set
            {
                if (_ViewModel != value)
                {
                    if (_ViewModel != null)
                    {
                        _ViewModel.StateHasChanged = null!;
                        _ViewModel.InvokeAsync = null!;
                    }

                    if (value != null)
                    {
                        value.StateHasChanged = () => InvokeAsync(StateHasChanged);
                        value.InvokeAsync = InvokeAsync;
                    }

                    _ViewModel = value!;
                }
            }
        }

        public virtual TViewModel VM => ViewModel;

        [Parameter(CaptureUnmatchedValues = true)]
        public virtual Dictionary<string, object> InputAttributes
        {
            set
            {
                if (value != null)
                {
                    var props = ViewModel.GetType()
                            .GetProperties().Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
                            .ToArray();

                    foreach (var parameter in value)
                    {
                        props.SingleOrDefault(p => string.Equals(p.Name, parameter.Key, StringComparison.InvariantCultureIgnoreCase))
                            ?.SetValue(ViewModel, parameter.Value);
                    }
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();

                await ViewModel.OnInitializedAsync();
            }
            catch (Exception exp)
            {
                VM.ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        protected override void OnParametersSet()
        {
            try
            {
                base.OnParametersSet();

                ViewModel.OnParametersSet();
            }
            catch (Exception exp)
            {
                VM.ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await base.OnParametersSetAsync();

                await ViewModel.OnParametersSetAsync();
            }
            catch (Exception exp)
            {
                VM.ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        protected override void OnInitialized()
        {
            try
            {
                base.OnInitialized();

                ViewModel.OnInitialized();
            }
            catch (Exception exp)
            {
                VM.ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        public virtual T Evaluate<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception exp)
            {
                VM.ExceptionHandler.OnExceptionReceived(exp);
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
                    VM.ExceptionHandler.OnExceptionReceived(exp);
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
                    Invoke(StateHasChanged); // workaround
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
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
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            };
        }

        private bool isDisposed;

        public virtual async ValueTask DisposeAsync()
        {
            if (isDisposed) return;
            await VM.DisposeAsync();
            isDisposed = true;
        }
    }
}
