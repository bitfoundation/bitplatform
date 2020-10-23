using Xamarin.Forms;

namespace Prism.Plugin.Popups.Dialogs.Xaml
{
    public static class PopupDialogLayout
    {
        public static readonly BindableProperty IsAnimationEnabledProperty =
            BindableProperty.CreateAttached("IsAnimationEnabled", typeof(bool?), typeof(PopupDialogLayout), null);

        public static bool? GetIsAnimationEnabled(BindableObject bindable) =>
            (bool?)bindable.GetValue(IsAnimationEnabledProperty);

        public static void SetIsAnimationEnabled(BindableObject bindable, bool? value) =>
            bindable.SetValue(IsAnimationEnabledProperty, value);
    }
}
