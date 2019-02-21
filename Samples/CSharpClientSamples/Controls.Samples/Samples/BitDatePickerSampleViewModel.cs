using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.CSharpClient.Controls.Samples
{
    public class BitDatePickerSampleViewModel : INotifyPropertyChanged
    {
        private DateTime _SelectedDate;

        public DateTime SelectedDate
        {
            get => _SelectedDate;
            set
            {
                _SelectedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
            }
        }

        public ICommand ChangeSelecedDate { get; set; }

        public BitDatePickerSampleViewModel()
        {
            SelectedDate = DateTime.Now;
            ChangeSelecedDate = new Command(() =>
            {
                SelectedDate = SelectedDate.AddDays(3);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
