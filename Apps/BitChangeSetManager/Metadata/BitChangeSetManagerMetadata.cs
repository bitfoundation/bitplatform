using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;

namespace BitChangeSetManager.Metadata
{
    public class BitChangeSetManagerMetadata : DefaultProjectMetadataBuilder
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddProjectMetadata(new ProjectMetadata
            {
                ProjectName = "BitChangeSetManager",
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Yes", Title = "Yes" },
                            new EnvironmentCultureValue { Name = "No", Title = "No" },
                            new EnvironmentCultureValue { Name = "Ok", Title = "Ok" },
                            new EnvironmentCultureValue { Name = "Commands", Title = "Commands" }
                        }
                    }
                }
            });

            return base.BuildMetadata();
        }
    }
}