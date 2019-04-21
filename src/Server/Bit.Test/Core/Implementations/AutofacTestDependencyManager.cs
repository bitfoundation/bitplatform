using Autofac;
using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Test.Implementations
{
    public class AutofacTestDependencyManager : AutofacDependencyManager
    {

        public override IDependencyManager Init()
        {
            base.Init();

            ContainerBuilder builder = GetContainerBuidler();

            builder.RegisterCallback(x =>
            {
                x.Registered += (sender, args) =>
                {
                    TypeInfo implementationType = args.ComponentRegistration.Activator.LimitType.GetTypeInfo();

                    if (IsGoingToCreateProxyForImplementationType(implementationType))
                    {
                        ConstructorInfo[] constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (constructors.Length == 1)
                        {
                            if (constructors.Single().GetParameters().Any())
                            {
                                throw new InvalidOperationException($"{implementationType.FullName} has only one constructor which has parameter");
                            }
                        }
                        else
                        {
                            if (constructors.All(c => !c.GetParameters().Any()))
                            {
                                throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, but all without parameter");
                            }

                            if (constructors.All(c => c.GetParameters().Any()))
                            {
                                throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, but all with parameter");
                            }

                            if (constructors.GroupBy(c => c.IsPublic).Count() == 1)
                            {
                                throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, all with same visibility level");
                            }

                            if (constructors.Single(c => !c.IsPublic).GetParameters().Any())
                            {
                                throw new InvalidOperationException($"{implementationType.FullName} has more than one constructor, and its non public constructor has parameter");
                            }
                        }

                        if (implementationType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Any(m => !m.IsVirtual))
                        {
                            throw new InvalidOperationException($"{implementationType.FullName} has non virtual public instance members");
                        }
                    }

                    args.ComponentRegistration.Activating += ComponentRegistration_Activating;
                };
            });

            return this;
        }

        public virtual List<Func<TypeInfo, bool>> AutoProxyCreationIncludeRules { get; set; } = new List<Func<TypeInfo, bool>> { };

        public virtual List<Func<TypeInfo, bool>> AutoProxyCreationIgnoreRules { get; set; } = new List<Func<TypeInfo, bool>> { };

        public virtual bool IsGoingToCreateProxyForImplementationType(TypeInfo implementationType)
        {
            return AutoProxyCreationIncludeRules.Any(rule => rule(implementationType) == true)
                && AutoProxyCreationIgnoreRules.All(rule => rule(implementationType) == false);
        }

        public virtual void ComponentRegistration_Activating(object sender, Autofac.Core.ActivatingEventArgs<object> e)
        {
            object instance = e.Instance;

            TypeInfo instanceType = instance.GetType().GetTypeInfo();

            if (IsGoingToCreateProxyForImplementationType(instanceType))
            {
                instance = _createProxyForService.MakeGenericMethod(instanceType).Invoke(this, new[] { instance });
                e.ReplaceInstance(instance);
            }

            Objects.Add(instance);
        }

        private readonly MethodInfo _createProxyForService = typeof(AutofacTestDependencyManager).GetTypeInfo().GetMethod(nameof(CreateProxyForService));

        public virtual T CreateProxyForService<T>(object serviceInstance)
            where T : class
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
