using Autofac;
using Prism.Ioc;
#if Android
using Autofac.Core;
using System;
using System.Collections.Generic;
using Xamarin.Forms.Internals;
#endif

namespace Prism.Autofac
{
    /// <summary>
    /// Application base class using Autofac
    /// </summary>
    public abstract class PrismApplication : PrismApplicationBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PrismApplication" /> using the default constructor
        /// </summary>
        protected PrismApplication()
            : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PrismApplication" /> with a <see cref="IPlatformInitializer" />.
        /// Used when there are specific types that need to be registered on the platform.
        /// </summary>
        /// <param name="platformInitializer">The <see cref="IPlatformInitializer"/>.</param>
        protected PrismApplication(IPlatformInitializer? platformInitializer)
            : base(platformInitializer) { }

        /// <summary>
        /// Initializes a new instance of <see cref="PrismApplication" /> with a <see cref="IPlatformInitializer" />.
        /// Used when there are specific types that need to be registered on the platform.
        /// Also determines whether to set the <see cref="DependencyResolver" /> for resolving Renderers and Platform Effects.
        /// </summary>
        /// <param name="platformInitializer">The <see cref="IPlatformInitializer"/>.</param>
        /// <param name="setFormsDependencyResolver">Should <see cref="PrismApplication" /> set the <see cref="DependencyResolver" />.</param>
        protected PrismApplication(IPlatformInitializer? platformInitializer, bool setFormsDependencyResolver)
            : base(platformInitializer, setFormsDependencyResolver) { }

        /// <summary>
        /// Creates the <see cref="IAutofacContainerExtension"/>
        /// </summary>
        /// <returns></returns>
        protected override IContainerExtension CreateContainerExtension()
        {
            return new AutofacContainerExtension(new ContainerBuilder());
        }
    }
}
