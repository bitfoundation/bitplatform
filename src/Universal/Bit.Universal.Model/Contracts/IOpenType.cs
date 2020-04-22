using System.Collections.Generic;

namespace Bit.Model.Contracts
{
    public interface IOpenType
    {
        Dictionary<string, object?> Properties { get; set; }
    }
}
