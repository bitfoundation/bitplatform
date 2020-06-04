using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.ViewModel;
using System.Linq;
using System.Reflection;

namespace Bit.View
{
    public class PageBase<TViewModel> : ComponentBase
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
                        _ViewModel.StateHasChanged = null!;

                    if (value != null)
                        value.StateHasChanged = StateHasChanged;

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

        protected async override Task OnInitializedAsync()
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

        protected async override Task OnParametersSetAsync()
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
            return new Func<Task>(async () =>
            {
                try
                {
                    await func();
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            });
        }


        public virtual Action Invoke(Action action)
        {
            return new Action(() =>
            {
                try
                {
                    action();
                    StateHasChanged(); // workaround
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            });
        }

        public virtual Func<EventArgs, Task> Invoke(Func<EventArgs, Task> func)
        {
            return new Func<EventArgs, Task>(async (e) =>
            {
                try
                {
                    await func(e);
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            });
        }
    }
}
