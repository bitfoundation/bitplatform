using Bit.CSharpClientSample.ViewModels;
using Rg.Plugins.Popup.Pages;

namespace Bit.CSharpClientSample.Views
{
    public partial class TestView : PopupPage
    {
        public TestView(TestViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }

        public TestViewModel ViewModel => (TestViewModel)BindingContext;

        /*protected override bool OnBackgroundClicked()
        {
            ViewModel.SomeCommandInViewModel.Execute(); // May be if you needed

            return base.OnBackgroundClicked();
        }*/
    }
}