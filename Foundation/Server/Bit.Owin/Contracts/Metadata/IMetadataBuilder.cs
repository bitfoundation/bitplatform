using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Api.Contracts.Metadata
{
    public class ObjectMetadata
    {

    }

    public interface IMetadataBuilder
    {
        Task<IEnumerable<ObjectMetadata>> BuildMetadata();
    }
}
