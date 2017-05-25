using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts.Metadata
{
    public class ObjectMetadata
    {

    }

    public interface IMetadataBuilder
    {
        Task<IEnumerable<ObjectMetadata>> BuildMetadata();
    }
}
