using System;
using System.Windows.Input;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    public class GoToNextViewCommandExtension : ICommand, IMarkupExtension<ICommand>
    {
        public Xamarin.Forms.View Next { get; set; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return Next != null;
        }

        public void Execute(object parameter)
        {
            Next?.Focus();
        }

        public ICommand ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
