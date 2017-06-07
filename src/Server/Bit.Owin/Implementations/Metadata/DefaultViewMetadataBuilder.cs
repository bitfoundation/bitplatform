using System;
using Bit.Owin.Contracts.Metadata;

namespace Bit.Owin.Implementations.Metadata
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
