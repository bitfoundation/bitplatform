using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Model.DomainModels;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;

namespace Bit.Owin.Metadata.Dtos
{
    public class UserSettingMetadataBuilder : DefaultDtoMetadataBuilder<UserSetting>
    {
        public override Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(UserSetting.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Theme), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.UserId), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Culture), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.DesiredTimeZone), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Version), new DtoMemberMetadata
            {

            });

            return base.BuildMetadata();
        }
    }
}
