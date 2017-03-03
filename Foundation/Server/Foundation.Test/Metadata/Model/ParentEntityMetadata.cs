using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using Foundation.Test.Model.DomainModels;
using System.Collections.Generic;

namespace Foundation.Test.Metadata.Model
{
    public class ParentEntityMetadata : DefaultDtoMetadataBuilder<ParentEntity>
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ParentEntity.Id), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Id" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Name), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Name" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Version), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Version" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Date), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Date" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.ChildEntities), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.TestModel), new DtoMemberMetadata
            {

            });

            return base.BuildMetadata();
        }
    }
}
