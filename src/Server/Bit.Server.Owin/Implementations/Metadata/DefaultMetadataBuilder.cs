using Bit.Owin.Contracts.Metadata;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations.Metadata
{
    public class DefaultMetadataBuilder : IMetadataBuilder
    {
        public virtual ICollection<ObjectMetadata> AllMetadata { get; protected set; } = new Collection<ObjectMetadata>();

        public virtual Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            return Task.FromResult((IEnumerable<ObjectMetadata>)AllMetadata);
        }
    }
}
