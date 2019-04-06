using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.CSharpClient.Controls.Samples
{
    public class BitDateTimePickerSampleViewModel : INotifyPropertyChanged
    {
        public DateTime SelectedDateTime { get; set; }

        public ICommand ChangeSelecedDate { get; set; }

        public BitDateTimePickerSampleViewModel()
        {
            SelectedDateTime = DateTime.Now;
            ChangeSelecedDate = new Command(() =>
            {
                SelectedDateTime = SelectedDateTime.AddDays(3);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
