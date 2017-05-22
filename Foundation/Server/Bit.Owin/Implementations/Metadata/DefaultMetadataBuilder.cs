using System.Collections.Generic;
using System.Collections.ObjectModel;
using Foundation.Api.Contracts.Metadata;
using System.Threading.Tasks;

namespace Foundation.Api.Implementations.Metadata
{
    public class DefaultMetadataBuilder : IMetadataBuilder
    {
        public virtual ICollection<ObjectMetadata> AllMetadata { get; protected set; } = new Collection<ObjectMetadata>();

        public virtual async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            return AllMetadata;
        }
    }
}
