using Autofac;
using Prism.Ioc;
using System;

namespace Prism.Autofac
{
    public static class PrismIocExtensions
    {
        public static IContainer GetContainer(this IContainerProvider containerProvider)
        {
            if (containerProvider == null)
                throw new ArgumentNullException(nameof(containerProvider));

            return ((IContainerExtension<IContainer>)containerProvider).Instance;
        }

        public static IContainer GetContainer(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            return ((IContainerExtension<IContainer>)containerRegistry).Instance;
        }

        /// <summary>
        /// Gets the <see cref="ContainerBuilder"/> used to register services.
        /// </summary>
        /// <param name="containerRegistry"></param>
        /// <returns>The current <see cref="ContainerBuilder"/></returns>
        public static ContainerBuilder GetBuilder(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            return ((IAutofacContainerExtension)containerRegistry).Builder;
        }
    }
}
