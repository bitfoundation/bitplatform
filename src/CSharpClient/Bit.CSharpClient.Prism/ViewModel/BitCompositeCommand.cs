using Prism.Commands;

namespace Bit.ViewModel
{
    public class BitCompositeCommand : CompositeCommand
    {
        public BitCompositeCommand()
        {

        }

        public BitCompositeCommand(bool monitorCommandActivity)
            : base(monitorCommandActivity)
        {

        }
    }
}
