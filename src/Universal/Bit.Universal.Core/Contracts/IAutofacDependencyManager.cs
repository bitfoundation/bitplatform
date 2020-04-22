using Autofac;

namespace Bit.Core.Contracts
{
    public interface IAutofacDependencyManager
    {
        ILifetimeScope GetContainer();

        ContainerBuilder GetContainerBuidler();

        void UseContainer(ILifetimeScope lifetimeScope);

        void UseContainerBuilder(ContainerBuilder containerBuilder);
    }
}
