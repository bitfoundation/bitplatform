using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace BitChangeSetManager.Xamarin
{
    public partial class App
	{
		public App ()
		{
			InitializeComponent();

			MainPage = new MainPage();
		}
	}
}
