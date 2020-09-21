using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;
using Bit.Tests.Model.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tests.Owin.Metadata.Dtos
{
    public class ValidationSampleDtoMetadata : DefaultDtoMetadataBuilder<ValidationSampleDto>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ValidationSampleDto.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ValidationSampleDto.RequiredByAttributeMember), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ValidationSampleDto.RequiredByMetadataMember), new DtoMemberMetadata
            {
                IsRequired = true
            });

            AddMemberMetadata(nameof(ValidationSampleDto.NotRequiredMember), new DtoMemberMetadata
            {

            });

            return await base.BuildMetadata();
        }
    }
}
