using System.Collections.Generic;

namespace Bit.Core.Exceptions.Contracts
{
    public interface IExceptionData
    {
        IDictionary<string, string?> Items { get; set; }
    }
}
