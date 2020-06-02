using Bit.Core.Contracts;
using Bit.Core.Implementations;

namespace Bit.ViewModel
{
    public class BitExceptionHandler : BitExceptionHandlerBase
    {
        public static new IExceptionHandler Current
        {
            get => BitExceptionHandlerBase.Current ??= new BitExceptionHandler();
            set => BitExceptionHandlerBase.Current = value;
        }
    }
}
