using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.CSharpClient.Controls.Samples
{
	public partial class MainPage : ContentPage
	{
		public MainPage ()
		{
			InitializeComponent ();
		}

        private async void DatePicker_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BitDatePickerSampleView());
        }

        private async void Checkbox_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BitCheckboxSampleView());
        }

        private async void RadioButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BitRadioButtonSampleView());
        }
    }
}