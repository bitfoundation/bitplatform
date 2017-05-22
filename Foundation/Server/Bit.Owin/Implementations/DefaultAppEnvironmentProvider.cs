using System;
using System.IO;
using System.Linq;
using Foundation.Core.Contracts;
using Foundation.Core.Models;

namespace Foundation.Api.Implementations
{
    public class DefaultAppEnvironmentProvider : IAppEnvironmentProvider
    {
        private static IAppEnvironmentProvider _current;
        private readonly IPathProvider _pathProvider;
        private readonly IContentFormatter _contentFormatter;

        private AppEnvironment _activeEnvironment;

        protected DefaultAppEnvironmentProvider()
        {
        }

        public DefaultAppEnvironmentProvider(IPathProvider pathProvider, IContentFormatter contentFormatter)
        {
            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            if (contentFormatter == null)
                throw new ArgumentNullException(nameof(contentFormatter));

            _pathProvider = pathProvider;
            _contentFormatter = contentFormatter;
        }

        public static IAppEnvironmentProvider Current
        {
            get
            {
                if (_current == null)
                    _current = new DefaultAppEnvironmentProvider(DefaultPathProvider.Current, DefaultJsonContentFormatter.Current);
                return _current;
            }
            set { _current = value; }
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            if (_activeEnvironment != null)
                return _activeEnvironment;

            string environmentsAsJson = File.ReadAllText(_pathProvider.MapPath("environments.json"));

            AppEnvironment[] allEnvironments = _contentFormatter.DeSerialize<AppEnvironment[]>(environmentsAsJson);

            AppEnvironment[] activeEnvironments = allEnvironments.Where(env => env.IsActive).ToArray();

            if (!activeEnvironments.Any())
                throw new InvalidOperationException("There is no active environment");

            if (activeEnvironments.Length > 1)
                throw new InvalidOperationException("There are more than one active environment");

            _activeEnvironment = activeEnvironments.Single();

            return _activeEnvironment;
        }
    }
}