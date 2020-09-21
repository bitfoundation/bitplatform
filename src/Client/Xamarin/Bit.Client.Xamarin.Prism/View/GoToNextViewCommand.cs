using Bit.ViewModel;

namespace Bit.View
{
    public class GoToNextViewCommand : BitDelegateCommand
    {
        public GoToNextViewCommand()
            : base(async () => { })
        {

        }

        protected override bool CanExecute(object parameter)
        {
            return Next != null;
        }

        protected override void Execute(object parameter)
        {
            Next!.Focus();
        }

        public Xamarin.Forms.View? Next { get; set; }
    }
}
