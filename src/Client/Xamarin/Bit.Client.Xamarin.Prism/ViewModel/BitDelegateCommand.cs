using Prism.Commands;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms.Xaml;

namespace Bit.ViewModel
{
    public class BitDelegateCommand : DelegateCommand, IMarkupExtension<ICommand>
    {
        private readonly Func<Task> _executeMethod = default!;

        public virtual async Task ExecuteAsync()
        {
            try
            {
                await _executeMethod();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public ICommand ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public BitDelegateCommand(Func<Task> executeMethod)
            : base(async () =>
            {
                try
                {
                    await executeMethod();
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
                    await executeMethod();
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

    public class BitDelegateCommand<T> : DelegateCommand<T>, IMarkupExtension<ICommand>
    {
        static BitDelegateCommand()
        {
            Type type = typeof(T);

            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                throw new InvalidOperationException($"Type {type.FullName} is not supported for BitDelegateCommand. Use either class or nullable strcut");
        }

        private readonly Func<T, Task> _executeMethod = default!;

        public virtual async Task ExecuteAsync(T parameter)
        {
            try
            {
                await _executeMethod(parameter);
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
                    await executeMethod(parameter);
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
                    await executeMethod(parameter);
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

        protected override void Execute(object? parameter)
        {
            parameter = ConvertParameterTypeIfRequired(parameter);

            base.Execute(parameter);
        }

        protected override bool CanExecute(object? parameter)
        {
            parameter = ConvertParameterTypeIfRequired(parameter);

            try
            {
                return base.CanExecute(parameter);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        protected object? ConvertParameterTypeIfRequired(object? parameter)
        {
            if (parameter != null && !(parameter is T) && parameter is IConvertible convertible)
            {
                Type type = typeof(T);
                type = Nullable.GetUnderlyingType(type) ?? type;
                parameter = convertible.ToType(type, provider: CultureInfo.InvariantCulture);
            }

            return parameter;
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
