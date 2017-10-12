using System;
using System.IO;
using System.Linq;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class DefaultAppEnvironmentProvider : IAppEnvironmentProvider
    {
        private AppEnvironment _activeEnvironment;
        private static IAppEnvironmentProvider _current;

        public virtual IPathProvider PathProvider { get; set; }
        public virtual IContentFormatter ContentFormatter { get; set; }

        public static IAppEnvironmentProvider Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new DefaultAppEnvironmentProvider
                    {
                        ContentFormatter = DefaultJsonContentFormatter.Current,
                        PathProvider = DefaultPathProvider.Current
                    };
                }
                return _current;
            }
            set => _current = value;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            if (_activeEnvironment != null)
                return _activeEnvironment;

            string environmentsAsJson = File.ReadAllText(PathProvider.MapPath("environments.json"));

            AppEnvironment[] allEnvironments = ContentFormatter.DeSerialize<AppEnvironment[]>(environmentsAsJson);

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