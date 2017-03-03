using BitChangeSetManager.Dto;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;

namespace BitChangeSetManager.Metadata
{
    public class ChangeSetDtoMetadata : DefaultDtoMetadataBuilder<ChangeSetDto>
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ChangeSetDto.Id), new DtoMemberMetadata
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

            AddMemberMetadata(nameof(ChangeSetDto.Title), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Title" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.CreatedOn), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name =  "Label" , Title = "Created on" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.Description), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Description" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.AssociatedCommitUrl), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Associated commit url" }
                        }
                    }
                }
            });

            return base.BuildMetadata();
        }
    }
}