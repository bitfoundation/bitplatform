using AutoMapper;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Model.Contracts;
using Bit.Tests.Api.ApiControllers;
using System;

namespace Bit.Tests.Model.Implementations
{
    public class TestMapperConfiguration : IMapperConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            if (AppEnvironment.TryGetConfig("UseAutoMapperProfile", out bool useAutoMapperProfile) && useAutoMapperProfile == true)
                return;

            mapperConfigExpression.CreateMap<SourceClass, DestClass>()
                 .ForMember(d => d.Id, config => config.MapFrom(s => s.Id * -1))
                 .ForMember(d => d.UserId, config => config.MapFrom<UserIdOfSourceDestClassesResolver>());
        }
    }

    public class TestProfile : Profile
    {
        protected TestProfile()
        {

        }

        public TestProfile(AppEnvironment appEnvironment)
        {
            if (appEnvironment == null)
                throw new ArgumentNullException(nameof(appEnvironment));

            if (appEnvironment.TryGetConfig("UseAutoMapperProfile", out bool useAutoMapperProfile) && useAutoMapperProfile == false)
                return;

            CreateMap<SourceClass, DestClass>()
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
