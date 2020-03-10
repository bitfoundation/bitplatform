using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Contracts
{
    public interface IExceptionHandler
    {
        void OnExceptionReceived(Exception exp, IDictionary<string, string> properties = null);
    }
}
