using System.ComponentModel;

namespace Bit.CSharpClient.Controls.Samples
{
    public class BitRadioButtonSampleViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Person Person { get; set; }

        public BitRadioButtonSampleViewModel()
        {
            Person = new Person
            {
                Name = "Yas",
                Gender = Gender.Man
            };
        }
    }

    public enum Gender
    {
        Man, Woman, Other
    }

    public class Person : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public Gender Gender { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
