using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Bit.CSharpClient.Controls.Samples
{
    public class BitDatePickerSampleViewModel :INotifyPropertyChanged
    {
        public DateTime? SelectedDate { get; set; }

        public BitDatePickerSampleViewModel()
        {
            SelectedDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
