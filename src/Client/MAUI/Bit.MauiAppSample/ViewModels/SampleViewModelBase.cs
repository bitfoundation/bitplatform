using System;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.MauiAppSample.Implementations;
using Bit.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Simple.OData.Client;

namespace Bit.MauiAppSample.ViewModels
{
    public class SampleViewModelBase : ViewModelBase
    {
        public SampleAuthenticationStateProvider RedemptionAuthenticationStateProvider { get; set; }

        [CascadingParameter]
        public Task<AuthenticationState> AuthenticationStateProvider { get; set; }

        public IJSRuntime JSRuntime { get; set; }
        public HttpClient HttpClient { get; set; }
        public IODataClient ODataClient { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public IContentFormatter ContentFormatter { get; set; }
        public NavigationManager NavigationManager { get; set; }
        public ODataBatch ODataBatch => ServiceProvider.GetRequiredService<ODataBatch>();
    }
}
