using BitChangeSetManager.Dto;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitChangeSetManager.Metadata
{
    public class ChangeSetDtoMetadata : DefaultDtoMetadataBuilder<ChangeSetDto>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
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
                            new EnvironmentCultureValue { Name = "Label" , Title = "Title" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "Title is required" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "Title's length is too big" }
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
                            new EnvironmentCultureValue { Name = "Label" , Title = "Description" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "Description's length is too big" }
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

            AddMemberMetadata(nameof(ChangeSetDto.IsDeliveredToAll), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Is delivered to all" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}