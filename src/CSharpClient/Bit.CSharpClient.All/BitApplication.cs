using Autofac;
using Prism;
using Prism.Autofac;
using Prism.Ioc;
using System;

namespace Bit
{
    public class BitAutofacContainerExtension : IAutofacContainerExtension
    {
        public BitAutofacContainerExtension(ContainerBuilder containerBuilder)
        {
            _autofacContainerExtension = new AutofacContainerExtension(containerBuilder);
        }

        private readonly AutofacContainerExtension _autofacContainerExtension;

        public ContainerBuilder Builder => _autofacContainerExtension.Builder;

        public IContainer Instance => _autofacContainerExtension.Instance;

        public bool SupportsModules => _autofacContainerExtension.SupportsModules;

        public void FinalizeExtension()
        {
            _autofacContainerExtension.GetType()
                .GetProperty(nameof(IAutofacContainerExtension.Instance))
                .SetValue(_autofacContainerExtension, Builder.Build());
        }

        public object ResolveViewModelForView(object view, Type viewModelType)
        {
            return _autofacContainerExtension.ResolveViewModelForView(view, viewModelType);
        }

        public object Resolve(Type type)
        {
            return _autofacContainerExtension.Resolve(type);
        }

        public object Resolve(Type type, string name)
        {
            return _autofacContainerExtension.Resolve(type, name);
        }

        public void RegisterInstance(Type type, object instance)
        {
            _autofacContainerExtension.RegisterInstance(type, instance);
        }

        public void RegisterSingleton(Type from, Type to)
        {
            _autofacContainerExtension.RegisterSingleton(from, to);
        }

        public void Register(Type from, Type to)
        {
            _autofacContainerExtension.Register(from, to);
        }

        public void Register(Type from, Type to, string name)
        {
            _autofacContainerExtension.Register(from, to, name);
        }
    }

    public abstract class BitApplication : PrismApplication
    {
        protected BitApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {

        }

        protected override IContainerExtension CreateContainerExtension()
        {
            return new BitAutofacContainerExtension(new ContainerBuilder());
        }
    }
}
