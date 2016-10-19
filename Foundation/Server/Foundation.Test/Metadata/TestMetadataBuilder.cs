using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;

namespace Foundation.Test.Metadata
{
    public class TestMetadataBuilder : DefaultProjectMetadataBuilder
    {
        public static readonly string SomeError = nameof(SomeError);

        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddProjectMetadata(new ProjectMetadata
            {
                ProjectName = "Foundation",
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = SomeError, Title = "یک خطا" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = SomeError, Title = "Some error" }
                        }
                    }
                }
            });

            return base.BuildMetadata();
        }
    }
}
