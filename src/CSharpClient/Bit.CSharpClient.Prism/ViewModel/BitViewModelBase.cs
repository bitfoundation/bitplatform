using Bit.Model;
using Bit.ViewModel.Contracts;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : Bindable, INavigatedAware, INavigatingAware, INavigationAware, IDestructible
    {
        public async void Destroy()
        {
            try
            {
                await DestroyAsync();
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task DestroyAsync()
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedFrom(INavigationParameters parameters)
        {
            try
            {
                await OnNavigatedFromAsync(parameters);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedTo(INavigationParameters parameters)
        {
            try
            {
                await Task.Yield();
                await OnNavigatedToAsync(parameters);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        protected virtual string GetViewModelName()
        {
            return GetType().Name.Replace("ViewModel", string.Empty);
        }

        protected virtual bool ShouldLogNavParam(string navParamName)
        {
            return false;
        }

        public async void OnNavigatingTo(INavigationParameters parameters)
        {
            DateTimeOffset startDate = DateTimeOffset.Now;
            bool success = true;

            try
            {
                await Task.Yield();
                await OnNavigatingToAsync(parameters);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                success = false;
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
            finally
            {
                if (parameters.GetNavigationMode() == NavigationMode.New)
                {
                    string pageName = GetViewModelName();

                    Dictionary<string, string> properties = new Dictionary<string, string> { };

                    foreach (KeyValuePair<string, object> prp in parameters)
                    {
                        if (ShouldLogNavParam(prp.Key))
                            properties.Add(prp.Key, prp.Value?.ToString() ?? "NULL");
                    }

                    properties.Add("PageViewSucceeded", success.ToString());

                    TimeSpan duration = DateTimeOffset.Now - startDate;

                    TelemetryServices.All().TrackPageView(pageName, duration, properties);
                }
            }
        }

        public virtual Task OnNavigatingToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public virtual INavService NavigationService { get; set; }

        public virtual IEnumerable<ITelemetryService> TelemetryServices { get; set; }
    }
}
