using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.ViewModel.Contracts;
using Prism.Navigation;
using Prism.Regions;
using Prism.Regions.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : Bindable, INavigatedAware, IInitializeAsync, INavigationAware, IDestructible, IRegionAware
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public CancellationToken CurrentCancellationToken { get; set; }

        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        public BitViewModelBase()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CurrentCancellationToken = CancellationTokenSource.Token;
        }

        public async void Destroy()
        {
            try
            {
                try
                {
                    CancellationTokenSource.Cancel();
                    CancellationTokenSource.Dispose();
                }
                finally // make sure that OnDestroyAsync gets called.
                {
                    await OnDestroyAsync();
                    await Task.Yield();
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnDestroyAsync()
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
                ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        protected virtual string GetViewModelName()
        {
            return GetType().Name.Replace("ViewModel", string.Empty);
        }

        protected virtual bool ShouldLogNavParam(string navParamName)
        {
            return true;
        }

        public async void OnNavigatedTo(INavigationParameters parameters)
        {
            DateTimeOffset startDate = DateTimeOffset.Now;
            bool success = true;
            string? navUri = null;

            try
            {
                await Task.Yield();
                await OnNavigatedToAsync(parameters);
                await Task.Yield();

                try
                {
                    navUri = NavigationService.GetNavigationUriPath() ?? GetType().Name;
                }
                catch
                {
                    navUri = GetType().Name;
                }
            }
            catch (Exception exp)
            {
                success = false;
                ExceptionHandler.OnExceptionReceived(exp);
            }
            finally
            {
                if (parameters.TryGetNavigationMode(out NavigationMode navigationMode) && navigationMode == NavigationMode.New)
                {
                    string pageName = GetViewModelName();

                    Dictionary<string, string?> properties = new Dictionary<string, string?> { };

                    foreach (KeyValuePair<string, object> prp in parameters)
                    {
                        if (ShouldLogNavParam(prp.Key) && prp.Key != KnownNavigationParameters.CreateTab && !properties.ContainsKey(prp.Key))
                            properties.Add(prp.Key, prp.Value?.ToString() ?? "NULL");
                    }

                    properties.Add("PageViewSucceeded", success.ToString(CultureInfo.InvariantCulture));
                    properties.Add("NavUri", navUri);

                    TimeSpan duration = DateTimeOffset.Now - startDate;

                    TelemetryServices.All().TrackPageView(pageName, duration, properties);
                }
            }
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync(INavigationParameters parameters)
        {
            try
            {
                await Task.Yield();
                OnInitialize(parameters);
            }
            catch (Exception exp)
            {
                ExceptionHandler.OnExceptionReceived(exp);
            }
        }

        public virtual void OnInitialize(INavigationParameters parameters)
        {
            
        }

        public void OnNavigatedTo(INavigationContext navigationContext)
        {
            OnNavigatedTo(navigationContext.Parameters);
        }

        public virtual bool IsNavigationTarget(INavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(INavigationContext navigationContext)
        {
            OnNavigatedFrom(navigationContext.Parameters);
        }

        public INavService NavigationService { get; set; } = default!;

        public IRegionManager RegionManager { get; set; }

        public IEnumerable<ITelemetryService> TelemetryServices { get; set; } = default!;
    }
}
