using Bit.ViewModel;
using Prism.Mvvm;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.View.Props
{
    public class PartialViewProps
    {
        public static BindableProperty PartialViewParentPageProperty = BindableProperty.CreateAttached(
            "PartialViewParentPage",
            typeof(Page),
            typeof(TemplatedView),
            defaultValue: null,
            propertyChanged: async (bindable, oldValue, newValue) =>
            {
                TemplatedView partialView = (TemplatedView)bindable;
                Page parentPage = (Page)newValue;

                ViewModelLocator.SetAutowirePartialView(partialView, parentPage);

                await Task.Yield();

                if (partialView.BindingContext is BitViewModelBase partialViewModel)
                    partialViewModel.NavigationService = ((BitViewModelBase)parentPage.BindingContext).NavigationService;
            });

        public static Page GetPartialViewParentPage(BindableObject view)
        {
            return (Page)view.GetValue(PartialViewParentPageProperty);
        }

        public static void SetPartialViewParentPage(BindableObject view, Page value)
        {
            view.SetValue(PartialViewParentPageProperty, value);
        }
    }
}
