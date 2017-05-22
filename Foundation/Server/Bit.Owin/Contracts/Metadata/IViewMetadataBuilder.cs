using Foundation.Core.Models;
using System.Collections.Generic;

namespace Foundation.Api.Contracts.Metadata
{
    public class ViewMetadata : ObjectMetadata
    {
        public virtual string ViewName { get; set; }

        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();
    }

    public interface IViewMetadataBuilder
    {
        IViewMetadataBuilder AddViewMetadata(ViewMetadata metadata);
    }
}
