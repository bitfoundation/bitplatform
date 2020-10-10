using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Prism.AppModel;
using Prism.Common;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Plugin.Popups;
using Prism.Services.Dialogs.Xaml;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;

namespace Prism.Services.Dialogs.Popups
{
    public class PopupDialogService : IDialogService
    {
        public const string PopupOverlayStyle = "PrismDialogMaskStyle";

        private IPopupNavigation _popupNavigation { get; }
        private IContainerExtension _containerExtension { get; }

        public PopupDialogService(IPopupNavigation popupNavigation, IContainerExtension containerExtension)
        {
            _popupNavigation = popupNavigation;
            _containerExtension = containerExtension;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need a catch all to return any exception in the Dialog Result")]
        public void ShowDialog(string name, IDialogParameters parameters, Action<IDialogResult> callback)
        {
            try
            {
                parameters = UriParsingHelper.GetSegmentParameters(name, parameters);

                var view = CreateViewFor(UriParsingHelper.GetSegmentName(name));
                var popupPage = CreatePopupPageForView(view);

                var dialogAware = InitializeDialog(view, parameters);

                if (!parameters.TryGetValue<bool>(KnownDialogParameters.CloseOnBackgroundTapped, out var closeOnBackgroundTapped))
                {
                    var dialogLayoutCloseOnBackgroundTapped = DialogLayout.GetCloseOnBackgroundTapped(view);
                    if (dialogLayoutCloseOnBackgroundTapped.HasValue)
                    {
                        closeOnBackgroundTapped = dialogLayoutCloseOnBackgroundTapped.Value;
                    }
                }

                dialogAware.RequestClose += DialogAware_RequestClose;

                void CloseOnBackgroundClicked(object sender, EventArgs args)
                {
                    DialogAware_RequestClose(new DialogParameters());
                }

                void DialogAware_RequestClose(IDialogParameters outParameters)
                {
                    try
                    {
                        var result = CloseDialog(outParameters ?? new DialogParameters(), popupPage, view);
                        if (result.Exception is DialogException de && de.Message == DialogException.CanCloseIsFalse)
                        {
                            return;
                        }

                        dialogAware.RequestClose -= DialogAware_RequestClose;
                        if (closeOnBackgroundTapped)
                        {
                            popupPage.BackgroundClicked -= CloseOnBackgroundClicked;
                        }
                        callback?.Invoke(result);
                        GC.Collect();
                    }
                    catch (DialogException dex)
                    {
                        if (dex.Message != DialogException.CanCloseIsFalse)
                        {
                            callback?.Invoke(new DialogResult
                            {
                                Exception = dex,
                                Parameters = parameters
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        callback?.Invoke(new DialogResult
                        {
                            Exception = ex,
                            Parameters = parameters
                        });
                    }
                }

                if (closeOnBackgroundTapped)
                {
                    popupPage.BackgroundClicked += CloseOnBackgroundClicked;
                }

                PushPopupPage(popupPage, view);
            }
            catch (Exception ex)
            {
                callback?.Invoke(new DialogResult { Exception = ex });
            }
        }

        private static PopupPage CreatePopupPageForView(BindableObject view)
        {
            var popupPage = new PopupPage();

            var hasSystemPadding = view.GetValue(Plugin.Popups.Popups.HasSystemPaddingProperty);
            if (hasSystemPadding != null) popupPage.HasSystemPadding = (bool)hasSystemPadding;

            var hasKeyboardOffset = view.GetValue(Plugin.Popups.Popups.HasKeyboardOffsetProperty);
            if (hasKeyboardOffset != null) popupPage.HasKeyboardOffset = (bool)hasKeyboardOffset;

            return popupPage;
        }

        private View CreateViewFor(string name)
        {
            var view = (View)_containerExtension.Resolve<object>(name);

            if (ViewModelLocator.GetAutowireViewModel(view) is null)
            {
                ViewModelLocator.SetAutowireViewModel(view, true);
            }

            return view;
        }

        private IDialogAware GetDialogController(View view)
        {
            if (view is IDialogAware viewAsDialogAware)
            {
                return viewAsDialogAware;
            }
            else if (view.BindingContext is null)
            {
                throw new DialogException(DialogException.NoViewModel);
            }
            else if (view.BindingContext is IDialogAware dialogAware)
            {
                return dialogAware;
            }

            throw new DialogException(DialogException.ImplementIDialogAware);
        }

        private IDialogAware InitializeDialog(View view, IDialogParameters parameters)
        {
            var dialog = GetDialogController(view);

            dialog.OnDialogOpened(parameters);

            if (dialog is IAbracadabra)
            {
                Abracadabra(dialog, parameters);
            }

            return dialog;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need a catch all to return any exception in the Dialog Result")]
        private IDialogResult CloseDialog(IDialogParameters parameters, PopupPage popupPage, View dialogView)
        {
            try
            {
                if (parameters is null)
                {
                    parameters = new DialogParameters();
                }

                var dialogAware = GetDialogController(dialogView);

                if (!dialogAware.CanCloseDialog())
                {
                    throw new DialogException(DialogException.CanCloseIsFalse);
                }

                dialogAware.OnDialogClosed();
                _popupNavigation.RemovePageAsync(popupPage);

                return new DialogResult
                {
                    Parameters = parameters
                };
            }
            catch (DialogException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new DialogResult
                {
                    Exception = ex,
                    Parameters = parameters
                };
            }
        }

        private async void PushPopupPage(PopupPage popupPage, View dialogView)
        {
            View mask = DialogLayout.GetMask(dialogView);

            if (mask is null)
            {
                Style overlayStyle = GetOverlayStyle(dialogView);

                mask = new BoxView
                {
                    Style = overlayStyle
                };
            }

            mask.SetBinding(VisualElement.WidthRequestProperty, new Binding { Path = "Width", Source = popupPage });
            mask.SetBinding(VisualElement.HeightRequestProperty, new Binding { Path = "Height", Source = popupPage });

            var overlay = new AbsoluteLayout();
            var relativeWidth = DialogLayout.GetRelativeWidthRequest(dialogView);
            if (relativeWidth != null)
            {
                dialogView.SetBinding(View.WidthRequestProperty,
                    new Binding("Width",
                                BindingMode.OneWay,
                                new RelativeContentSizeConverter { RelativeSize = relativeWidth.Value },
                                source: popupPage));
            }

            var relativeHeight = DialogLayout.GetRelativeHeightRequest(dialogView);
            if (relativeHeight != null)
            {
                dialogView.SetBinding(View.HeightRequestProperty,
                    new Binding("Height",
                                BindingMode.OneWay,
                                new RelativeContentSizeConverter { RelativeSize = relativeHeight.Value },
                                source: popupPage));
            }

            //AbsoluteLayout.SetLayoutFlags(content, AbsoluteLayoutFlags.PositionProportional);
            //AbsoluteLayout.SetLayoutBounds(content, new Rectangle(0f, 0f, popupPage.Width, popupPage.Height));
            AbsoluteLayout.SetLayoutFlags(dialogView, AbsoluteLayoutFlags.PositionProportional);
            var popupBounds = DialogLayout.GetLayoutBounds(dialogView);
            AbsoluteLayout.SetLayoutBounds(dialogView, popupBounds);
            //overlay.Children.Add(content);
            if (DialogLayout.GetUseMask(dialogView) ?? true)
            {
                overlay.Children.Add(mask);
            }

            overlay.Children.Add(dialogView);
            popupPage.Content = overlay;
            await _popupNavigation.PushAsync(popupPage);
        }

        private static Style GetOverlayStyle(View popupView)
        {
            var style = DialogLayout.GetMaskStyle(popupView);
            if (style != null)
            {
                return style;
            }

            if (Application.Current.Resources.ContainsKey(PopupOverlayStyle))
            {
                style = (Style)Application.Current.Resources[PopupOverlayStyle];
                if (style.TargetType == typeof(BoxView))
                {
                    return style;
                }
            }

            var overlayStyle = new Style(typeof(BoxView));
            overlayStyle.Setters.Add(new Setter { Property = VisualElement.OpacityProperty, Value = 0.75 });
            overlayStyle.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.Black });

            Application.Current.Resources[PopupOverlayStyle] = overlayStyle;
            return overlayStyle;
        }


        private class DialogResult : IDialogResult
        {
            public Exception Exception { get; set; }
            public IDialogParameters Parameters { get; set; }
        }

        private static void Abracadabra(object page, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var props = page.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(x => x.CanWrite);

            foreach (var prop in props)
            {
                (var name, var isRequired) = prop.GetAutoInitializeProperty();

                if (!parameters.HasKey(name, out var key))
                {
                    if (isRequired)
                        throw new ArgumentNullException(name);
                    continue;
                }

                prop.SetValue(page, parameters.GetValue(key, prop.PropertyType));
            }
        }
    }
}
