using Autofac;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Bit.Data.NHibernate.Implementations;
using Bit.Owin.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NHibernate;
using NHibernate.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterNHibernate(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            DefaultJsonContentFormatter.SerializeSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ContractResolver = new NHibernateContractResolver { },
                Converters = new List<JsonConverter> { new StringEnumConverter { } }
            };

            dependencyManager.Register<IDataProviderSpecificMethodsProvider, NHibernateDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<NHibernateDataProviderSpecificMethodsProvider, NHibernateDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IUnitOfWork, DefaultUnitOfWork>(overwriteExisting: false);

            ((IAutofacDependencyManager)dependencyManager)
                .GetContainerBuidler()
                .Register(context =>
                {
                    ISession session = context.Resolve<ISessionFactory>().OpenSession();
                    session.BeginTransaction((IsolationLevel)Enum.Parse(typeof(IsolationLevel), context.Resolve<AppEnvironment>().GetConfig(AppEnvironment.KeyValues.Data.DbIsolationLevel, AppEnvironment.KeyValues.Data.DbIsolationLevelDefaultValue)!));
                    Scopes.TryAdd(((SessionImpl)session).SessionId, context.Resolve<IScopeStatusManager>());
                    return session;
                })
                .InstancePerLifetimeScope()
                .OnRelease(session =>
                {
                    try
                    {
                        if (Scopes.TryRemove(((SessionImpl)session).SessionId, out IScopeStatusManager? scopeStatusManager) && scopeStatusManager.WasSucceeded())
                            session.Transaction.Commit();
                        else
                            session.Transaction.Rollback();
                    }
                    finally
                    {
                        session.Dispose();
                    }
                });

            return dependencyManager;
        }

        private static readonly ConcurrentDictionary<Guid, IScopeStatusManager> Scopes = new ConcurrentDictionary<Guid, IScopeStatusManager>();
    }
}
