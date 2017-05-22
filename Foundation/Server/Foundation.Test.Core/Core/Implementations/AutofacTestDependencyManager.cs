using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Api.Contracts;
using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using Foundation.Api.Implementations;
using Foundation.Api.Metadata;
using Foundation.Api.ApiControllers;

namespace Foundation.Test.Core.Implementations
{
    public class AutofacTestDependencyManager : AutofacDependencyManager
    {
        public AutofacTestDependencyManager()
        {
            AutoProxyCreationIgnoreRules = new List<Func<TypeInfo, bool>>
            {
                serviceType => GetBaseTypes(serviceType).Any(t => t.Name == "DbContext"),
                serviceType => GetBaseTypes(serviceType).Any(t => t.Name == "Hub"),
                serviceType => serviceType.IsArray,
                serviceType => serviceType.IsInterface
            };
        }

        public override IDependencyManager Init()
        {
            base.Init();

            ContainerBuilder builder = GetContainerBuidler();

            builder.RegisterCallback(x =>
            {
                x.Registered += (sender, args) =>
                {
                    TypeInfo serviceType = args.ComponentRegistration.Activator.LimitType.GetTypeInfo();

                    if (IsGoingToCreateProxyForService(serviceType))
                    {
                        ConstructorInfo[] constructors = serviceType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (constructors.Length == 1)
                        {
                            if (constructors.Single().GetParameters().Any())
                            {
                                throw new InvalidOperationException($"{serviceType.FullName} has only one constructor which has parameter");
                            }
                        }
                        else
                        {
                            if (constructors.All(c => !c.GetParameters().Any()))
                            {
                                throw new InvalidOperationException($"{serviceType.FullName} has more than one constructor, but all without parameter");
                            }

                            if (constructors.All(c => c.GetParameters().Any()))
                            {
                                throw new InvalidOperationException($"{serviceType.FullName} has more than one constructor, but all with parameter");
                            }

                            if (constructors.GroupBy(c => c.IsPublic).Count() == 1)
                            {
                                throw new InvalidOperationException($"{serviceType.FullName} has more than one constructor, all with same visibility level");
                            }

                            if (constructors.Single(c => !c.IsPublic).GetParameters().Any())
                            {
                                throw new InvalidOperationException($"{serviceType.FullName} has more than one constructor, and its non public constructor has parameter");
                            }
                        }

                        if (serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Any(m => !m.IsVirtual))
                        {
                            throw new InvalidOperationException($"{serviceType.FullName} has non virtual public instance members");
                        }
                    }

                    args.ComponentRegistration.Activating += ComponentRegistration_Activating;
                };
            });

            return this;
        }

        public readonly List<Func<TypeInfo, bool>> AutoProxyCreationIncludeRules = new List<Func<TypeInfo, bool>>
        {
            serviceType => typeof(IDateTimeProvider).GetTypeInfo().Assembly == serviceType.Assembly,
            serviceType => typeof(IOwinMiddlewareConfiguration).GetTypeInfo().Assembly == serviceType.Assembly,
            serviceType => typeof(IRepository<>).GetTypeInfo().Assembly == serviceType.Assembly,
            serviceType => typeof(IEntity).GetTypeInfo().Assembly == serviceType.Assembly,
            serviceType => typeof(AutofacTestDependencyManager).GetTypeInfo().Assembly == serviceType.Assembly,
            serviceType => typeof(DtoController<>).GetTypeInfo().Assembly == serviceType.Assembly
        };

        public readonly List<Func<TypeInfo, bool>> AutoProxyCreationIgnoreRules;

        private IEnumerable<Type> GetBaseTypes(Type type)
        {
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        public virtual bool IsGoingToCreateProxyForService(TypeInfo serviceType)
        {
            return AutoProxyCreationIncludeRules.Any(rule => rule(serviceType) == true)
                && AutoProxyCreationIgnoreRules.All(rule => rule(serviceType) == false);
        }

        public virtual void ComponentRegistration_Activating(object sender, Autofac.Core.ActivatingEventArgs<object> e)
        {
            object instance = e.Instance;

            TypeInfo instanceType = instance.GetType().GetTypeInfo();

            if (IsGoingToCreateProxyForService(instanceType))
            {
                instance = _createProxyForService.MakeGenericMethod(instanceType).Invoke(this, new[] { instance });
                e.ReplaceInstance(instance);
            }

            Objects.Add(instance);
        }

        private readonly MethodInfo _createProxyForService = typeof(AutofacTestDependencyManager).GetTypeInfo().GetMethod(nameof(CreateProxyForService));

        public virtual T CreateProxyForService<T>(object serviceInstance)
        {
            T originalInstance = (T)serviceInstance;

            serviceInstance = A.Fake<T>(x =>
            {
                x.Wrapping(originalInstance);
            });

            return (T)serviceInstance;
        }

        public readonly List<object> Objects = new List<object>();

        public virtual string ReportObjects
        {
            get { return string.Join(Environment.NewLine, Objects.Select(o => o.GetType().FullName)); }
        }
    }
}
