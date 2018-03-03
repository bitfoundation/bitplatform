using Prism.Commands;
using System;

namespace Bit.ViewModel
{
    public class BitDelegateCommand : DelegateCommand
    {
        public BitDelegateCommand(Action executeMethod) 
            : base(executeMethod)
        {
        }

        public BitDelegateCommand(Action executeMethod, Func<bool> canExecuteMethod) 
            : base(executeMethod, canExecuteMethod)
        {
        }
    }

    public class BitDelegateCommand<T> : DelegateCommand<T>
        where T : class
    {
        public BitDelegateCommand(Action<T> executeMethod)
            : base(executeMethod)
        {

        }

        public BitDelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {

        }
    }
}
