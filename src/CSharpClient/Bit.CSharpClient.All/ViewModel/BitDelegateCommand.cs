using Prism.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitDelegateCommand : DelegateCommand
    {
        private readonly Func<Task> _executeMethod;

        public virtual async Task ExecuteAsync()
        {
            try
            {
                await _executeMethod().ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public BitDelegateCommand(Func<Task> executeMethod)
            : base(async () =>
            {
                try
                {
                    await executeMethod().ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            })
        {
            if (executeMethod == null)
                throw new ArgumentNullException(nameof(executeMethod));

            _executeMethod = executeMethod;
        }

        public BitDelegateCommand(Func<Task> executeMethod, Func<bool> canExecuteMethod)
            : base(async () =>
            {
                try
                {
                    await executeMethod().ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, canExecuteMethod)
        {
            if (executeMethod == null)
                throw new ArgumentNullException(nameof(executeMethod));

            _executeMethod = executeMethod;
        }
    }

    public class BitDelegateCommand<T> : DelegateCommand<T>
        where T : class
    {
        private readonly Func<T, Task> _executeMethod;

        public virtual async Task ExecuteAsync(T parameter)
        {
            try
            {
                await _executeMethod(parameter).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public BitDelegateCommand(Func<T, Task> executeMethod)
            : base(async (parameter) =>
            {
                try
                {
                    await executeMethod(parameter).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            })
        {
            if (executeMethod == null)
                throw new ArgumentNullException(nameof(executeMethod));

            _executeMethod = executeMethod;
        }

        public BitDelegateCommand(Func<T, Task> executeMethod, Func<T, bool> canExecuteMethod)
            : base(async (parameter) =>
            {
                try
                {
                    await executeMethod(parameter).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, canExecuteMethod)
        {
            if (executeMethod == null)
                throw new ArgumentNullException(nameof(executeMethod));

            _executeMethod = executeMethod;
        }
    }
}
