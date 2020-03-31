using System.Collections.Generic;

namespace Bit.ViewModel.Contracts
{
    public interface IExceptionData
    {
        IDictionary<string, string> Items { get; set; }
    }
}
