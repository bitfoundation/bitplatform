using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Test.Model.Dto;
using System.Collections.Generic;

namespace Foundation.Test.Metadata.Dtos
{
    public class ValidationSampleDtoMetadata : DefaultDtoMetadataBuilder<ValidationSampleDto>
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
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

            return base.BuildMetadata();
        }
    }
}
