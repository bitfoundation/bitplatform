using Bit.Core.Models;
using System.Collections.Generic;

namespace Bit.Owin.Contracts.Metadata
{
    public class ProjectMetadata : ObjectMetadata
    {
        public virtual string ProjectName { get; set; }

        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();
    }

    public interface IProjectMetadataBuilder
    {
        IProjectMetadataBuilder AddProjectMetadata(ProjectMetadata metadata);
    }
}
