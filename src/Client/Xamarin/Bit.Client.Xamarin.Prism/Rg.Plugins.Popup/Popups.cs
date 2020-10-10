using Xamarin.Forms;

namespace Prism.Plugin.Popups
{
    public static class Popups
    {
        public static readonly BindableProperty HasSystemPaddingProperty = BindableProperty.CreateAttached("HasSystemPadding", typeof(bool), typeof(View), false);

        public static bool GetHasSystemPadding(BindableObject view)
        {
            return (bool)view.GetValue(HasSystemPaddingProperty);
        }

        public static void SetHasSystemPadding(BindableObject view, bool value)
        {
            view.SetValue(HasSystemPaddingProperty, value);
        }

        public static readonly BindableProperty HasKeyboardOffsetProperty = BindableProperty.CreateAttached("HasKeyboardOffset", typeof(bool), typeof(View), false);

        public static bool GetHasKeyboardOffset(BindableObject view)
        {
            return (bool)view.GetValue(HasKeyboardOffsetProperty);
        }

        public static void SetHasKeyboardOffset(BindableObject view, bool value)
        {
            view.SetValue(HasKeyboardOffsetProperty, value);
        }
    }
}
