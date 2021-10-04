using System;
using System.Threading.Tasks;
using Bit.MauiAppSample.ViewModels;
using Bit.View;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Bit.MauiAppSample.Pages
{
    [Authorize("IsLoggedIn")]
    public class SamplePageBase<TViewModel> : PageBase<TViewModel>
        where TViewModel : SampleViewModelBase
    {
        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateProvider
        {
            get => VM.AuthenticationStateProvider;
            set => VM.AuthenticationStateProvider = value;
        }

        public Func<T, Task> Invoke<T>(Func<T, Task> func)
        {
            return new Func<T, Task>(async (ec) =>
            {
                try
                {
                    await func(ec);
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            });
        }

        public Action<T> Invoke<T>(Action<T> action)
        {
            return new Action<T>((ec) =>
            {
                try
                {
                    action(ec);
                }
                catch (Exception exp)
                {
                    VM.ExceptionHandler.OnExceptionReceived(exp);
                }
            });
        }
    }
}
