using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Bit.ViewModel;
using System.Linq;
using System.Reflection;

namespace Bit.View
{
    public class PageBase<TViewModel> : ComponentBase, INotifyPropertyChanged
         where TViewModel : ViewModelBase
    {
        private TViewModel _ViewModel = default!;

        [Inject]
        public TViewModel ViewModel
        {
            get => _ViewModel;
            set
            {
                if (_ViewModel != value)
                {
                    _ViewModel.StateHasChanged = null!;

                    if (value != null)
                        value.StateHasChanged = StateHasChanged;

                    _ViewModel = value!;
                }
            }
        }

        public TViewModel VM => ViewModel;

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> InputAttributes
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

        public event PropertyChangedEventHandler PropertyChanged;

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

        public T Evaluate<T>(Func<T> func)
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

        public Func<Task> Invoke(Func<Task> func)
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


        public Action Invoke(Action action)
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
    }
}
