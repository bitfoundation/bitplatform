using Autofac;

namespace Foundation.Api.Contracts
{
    public interface IAutofacDependencyManager
    {
        ILifetimeScope GetContainer();

        ContainerBuilder GetContainerBuidler();
    }
}
