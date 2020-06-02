using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class ViewModelBase : Bindable
    {
        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        public Action StateHasChanged = default!;

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
    }
}
