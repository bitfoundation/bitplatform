using AutoMapper;
using Bit.Core.Contracts;
using Bit.Model.Contracts;
using System.Reflection;

namespace Bit.Model.Implementations
{
    public class DefaultMapperConfiguration : IMapperConfiguration
    {
        public virtual IDependencyManager DependencyManager { get; set; }

        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.ValidateInlineMaps = false;
            mapperConfigExpression.CreateMissingTypeMaps = true;
        }
    }
}
