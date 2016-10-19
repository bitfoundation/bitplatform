using System.Collections.Generic;

namespace Foundation.Api.Contracts.Metadata
{
    public class ObjectMetadata
    {

    }

    public interface IMetadataBuilder
    {
        IEnumerable<ObjectMetadata> BuildMetadata();
    }
}
