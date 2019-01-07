using Prism;
using Prism.Behaviors;
using Prism.Common;
using Prism.Ioc;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Plugin.Popups;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.ViewModel.Implementations
{
    internal delegate Page TopPage(IPopupNavigation popupNavigation, IApplicationProvider applicationProvider);
    internal delegate Page GetOnNavigatedToTarget(IPopupNavigation popupNavigation, IApplicationProvider applicationProvider);

    internal static class PopupUtilities
    {
        private static readonly Lazy<TopPage> TopPageInstance = new Lazy<TopPage>(() =>
        {
            return (TopPage)typeof(PopupPageNavigationService)
                .Assembly
                .GetType("Prism.Plugin.Popups.PopupUtilities")
                .GetMethod(nameof(TopPage))
                .CreateDelegate(typeof(TopPage));
        });

        public static Page TopPage(IPopupNavigation popupNavigation, IApplicationProvider applicationProvider)
        {
            return TopPageInstance.Value(popupNavigation, applicationProvider);
        }

        private static readonly Lazy<GetOnNavigatedToTarget> GetOnNavigatedToTargetInstance = new Lazy<GetOnNavigatedToTarget>(() =>
        {
            return (GetOnNavigatedToTarget)typeof(PopupPageNavigationService)
                .Assembly
                .GetType("Prism.Plugin.Popups.PopupUtilities")
                .GetMethod(nameof(GetOnNavigatedToTarget))
                .CreateDelegate(typeof(GetOnNavigatedToTarget));
        });

        public static Page GetOnNavigatedToTarget(IPopupNavigation popupNavigation, IApplicationProvider applicationProvider)
        {
            return GetOnNavigatedToTargetInstance.Value(popupNavigation, applicationProvider);
        }
    }

    /// <summary>
    /// https://github.com/dansiegel/Prism.Plugin.Popups/pull/84
    /// </summary>
    public class BitPopupPageNavigationService : PageNavigationService
    {
        protected IPopupNavigation _popupNavigation { get; }

        public BitPopupPageNavigationService(IPopupNavigation popupNavigation, IContainerExtension container,
                                          IApplicationProvider applicationProvider, IPageBehaviorFactory pageBehaviorFactor,
                                          ILoggerFacade logger)
            : base(container, applicationProvider, pageBehaviorFactor, logger)
        {
            _popupNavigation = popupNavigation;
        }

        protected override async Task<INavigationResult> GoBackInternal(INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            INavigationResult result = null;
            try
            {
                NavigationSource = PageNavigationSource.NavigationService;

                switch (PopupUtilities.TopPage(_popupNavigation, _applicationProvider))
                {
                    case PopupPage popupPage:
                        var segmentParameters = UriParsingHelper.GetSegmentParameters(null, parameters);
                        ((INavigationParametersInternal)segmentParameters).Add("__NavigationMode", NavigationMode.Back);
                        var previousPage = PopupUtilities.GetOnNavigatedToTarget(_popupNavigation, _applicationProvider);

                        PageUtilities.OnNavigatingTo(previousPage, segmentParameters);
                        await DoPop(popupPage.Navigation, false, animated);

                        if (popupPage != null)
                        {
                            PageUtilities.InvokeViewAndViewModelAction<IActiveAware>(popupPage, a => a.IsActive = false);
                            PageUtilities.OnNavigatedFrom(popupPage, segmentParameters);
                            PageUtilities.OnNavigatedTo(previousPage, segmentParameters);
                            await InvokeOnNavigatedToAsync(previousPage, segmentParameters);
                            PageUtilities.InvokeViewAndViewModelAction<IActiveAware>(previousPage, a => a.IsActive = true);
                            PageUtilities.DestroyPage(popupPage);
                            result = new NavigationResult { Success = true };
                            break;
                        }
                        throw new NullReferenceException("The PopupPage was null following the Pop");
                    default:
                        result = await base.GoBackInternal(parameters, useModalNavigation, animated);
                        break;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                _logger.Log(e.ToString(), Category.Exception, Priority.High);
                result = new NavigationResult { Success = false, Exception = e }; ;
            }
            finally
            {
                NavigationSource = PageNavigationSource.Device;
            }
            return result;
        }

        protected override async Task<Page> DoPop(INavigation navigation, bool useModalNavigation, bool animated)
        {
            if (_popupNavigation.PopupStack.Count > 0)
            {
                await _popupNavigation.PopAsync(animated);
                return null;
            }

            return await base.DoPop(navigation, useModalNavigation, animated);
        }

        protected override async Task DoPush(Page currentPage, Page page, bool? useModalNavigation, bool animated, bool insertBeforeLast = false, int navigationOffset = 0)
        {
            switch (page)
            {
                case PopupPage popup:
                    await _popupNavigation.PushAsync(popup, animated);
                    break;
                default:
                    await base.DoPush(currentPage, page, useModalNavigation, animated, insertBeforeLast, navigationOffset);
                    if (_popupNavigation.PopupStack.Any())
                        await _popupNavigation.PopAllAsync(false);
                    break;
            }
        }

        protected override Page GetCurrentPage()
        {
            if (_popupNavigation.PopupStack.Any())
            {
                return _popupNavigation.PopupStack.LastOrDefault();
            }

            return base.GetCurrentPage();
        }

        private async Task InvokeOnNavigatedToAsync(object view, INavigationParameters parameters)
        {
            if (view is INavigatedAwareAsync navigatedAware)
            {
                await navigatedAware.OnNavigatedToAsync(parameters);
            }

            if (view is BindableObject bindable && bindable.BindingContext is INavigatedAwareAsync vm)
            {
                await vm.OnNavigatedToAsync(parameters);
            }

#if !NETSTANDARD1_0
            if (view is Page page)
            {
                BindableProperty partialViewsProperty = typeof(ViewModelLocator).GetField("PartialViewsProperty", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as BindableProperty;
                var partials = (List<BindableObject>)page.GetValue(partialViewsProperty);
                foreach (var partial in partials ?? new List<BindableObject>())
                {
                    await InvokeOnNavigatedToAsync(partial, parameters);
                }
            }
#endif
        }
    }
}
