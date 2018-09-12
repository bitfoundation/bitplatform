using Xamarin.Forms;

namespace Bit.View
{
    public class BitNavigationPage : NavigationPage
    {
        public static readonly BindableProperty NavBarProperty = BindableProperty.CreateAttached(
            propertyName: "NavBar",
            returnType: typeof(Xamarin.Forms.View),
            declaringType: typeof(Page),
            defaultValue: null);

        public static Xamarin.Forms.View GetNavBar(BindableObject page) => (Xamarin.Forms.View)page.GetValue(NavBarProperty);

        public static void SetNavBar(BindableObject page, Xamarin.Forms.View view) => page.SetValue(NavBarProperty, view);

        public BitNavigationPage()
        {
        }

        public BitNavigationPage(Page root)
            : base(root)
        {
        }
    }
}
