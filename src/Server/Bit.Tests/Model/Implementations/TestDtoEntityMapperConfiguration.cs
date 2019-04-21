using AutoMapper;
using Bit.Core.Contracts;
using Bit.Model.Contracts;
using Bit.Tests.Api.ApiControllers;

namespace Bit.Tests.Model.Implementations
{
    public class TestMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMap<SourceClass, DestClass>()
                .ForMember(d => d.Id, config => config.MapFrom(s => s.Id * -1))
                .ForMember(d => d.UserId, config => config.MapFrom<UserIdOfSourceDestClassesResolver>());
        }
    }

    public class UserIdOfSourceDestClassesResolver : IValueResolver<SourceClass, DestClass, string>
    {
        // To test per scope access to something which depends on current request.
        public virtual IUserInformationProvider UserInformationProvider { get; set; }

        public string Resolve(SourceClass source, DestClass destination, string member, ResolutionContext context)
        {
            return UserInformationProvider.GetCurrentUserId();
        }
    }
}
