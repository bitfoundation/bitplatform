using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class ViewModelBase : Bindable, IAsyncDisposable
    {
        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        public Func<Task> StateHasChanged = default!;

        public Func<Action, Task> InvokeAsync = default!;

        public virtual Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void OnInitialized()
        {

        }

        public virtual Task OnParametersSetAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void OnParametersSet()
        {

        }

        public virtual async ValueTask DisposeAsync()
        {

        }
    }
}
