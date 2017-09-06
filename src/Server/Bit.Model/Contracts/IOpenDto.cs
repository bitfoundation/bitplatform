using System.Collections.Generic;

namespace Bit.Model.Contracts
{
    public interface IOpenDto : IDto
    {
        Dictionary<string, object> Properties { get; set; }
    }
}
