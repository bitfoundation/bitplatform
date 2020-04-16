using AutoMapper;
using Bit.Core.Contracts;
using Bit.Model.Contracts;

namespace Bit.Model.Implementations
{
    public class DefaultMapperConfiguration : IMapperConfiguration
    {
        public virtual IDependencyManager DependencyManager { get; set; } = default!;

        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {

        }
    }
}
