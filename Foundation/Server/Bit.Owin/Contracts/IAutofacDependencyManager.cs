using Autofac;

namespace Bit.Owin.Contracts
{
    public interface IAutofacDependencyManager
    {
        ILifetimeScope GetContainer();

        ContainerBuilder GetContainerBuidler();

        void UseContainer(ILifetimeScope lifetimeScope);

        void UseContainerBuilder(ContainerBuilder containerBuilder);
    }
}
