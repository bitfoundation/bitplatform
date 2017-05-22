using Foundation.Api.Contracts.Metadata;
using System;

namespace Foundation.Api.Implementations.Metadata
{
    public class DefaultProjectMetadataBuilder : DefaultMetadataBuilder, IProjectMetadataBuilder
    {
        public virtual IProjectMetadataBuilder AddProjectMetadata(ProjectMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            AllMetadata.Add(metadata);

            return this;
        }
    }
}
