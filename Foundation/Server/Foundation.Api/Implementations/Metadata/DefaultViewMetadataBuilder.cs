using Foundation.Api.Contracts.Metadata;
using System;

namespace Foundation.Api.Implementations.Metadata
{
    public class DefaultViewMetadataBuilder : DefaultMetadataBuilder, IViewMetadataBuilder
    {
        public virtual IViewMetadataBuilder AddViewMetadata(ViewMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            AllMetadata.Add(metadata);

            return this;
        }
    }
}
