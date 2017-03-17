using BitChangeSetManager.Dto;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitChangeSetManager.Metadata
{
    public class DeliveryDtoMetadata : DefaultDtoMetadataBuilder<DeliveryDto>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(DeliveryDto.Id), new DtoMemberMetadata
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

            AddMemberMetadata(nameof(DeliveryDto.CustomerName), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Customer name" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(DeliveryDto.DeliveredOn), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Delivered on" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}