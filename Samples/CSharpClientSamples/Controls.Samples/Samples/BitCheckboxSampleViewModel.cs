using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.CSharpClient.Controls.Samples
{
    public class BitCheckboxSampleViewModel : INotifyPropertyChanged
    {
        public ICommand ChangeIsChecked { get; set; }
        public ICommand IsCheckedChanged { get; set; }
        public bool IsChecked { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitCheckboxSampleViewModel()
        {
            ChangeIsChecked = new Command(() => IsChecked = !IsChecked);

            IsCheckedChanged = new Command<bool>(isChecked =>
            {

            });
        }
    }
}
