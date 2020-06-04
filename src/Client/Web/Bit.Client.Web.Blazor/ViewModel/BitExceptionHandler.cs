using Bit.Core.Contracts;
using Bit.Core.Implementations;
using System;

namespace Bit.ViewModel
{
    public class BitExceptionHandler : BitExceptionHandlerBase
    {
        public virtual IServiceProvider? ServiceProvider { get; set; }

        public static new IExceptionHandler Current
        {
            get => BitExceptionHandlerBase.Current ??= new BitExceptionHandler();
            set => BitExceptionHandlerBase.Current = value;
        }
    }
}
