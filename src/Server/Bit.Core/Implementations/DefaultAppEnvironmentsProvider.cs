using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.IO;
using System.Linq;

namespace Bit.Core.Implementations
{
    public class DefaultAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        private AppEnvironment _activeAppEnvironment;
        private static IAppEnvironmentsProvider _current;

        public virtual IPathProvider PathProvider { get; set; }
        public virtual IContentFormatter ContentFormatter { get; set; }

        public static IAppEnvironmentsProvider Current
        {
            get
            {
                return _current ?? (_current = new DefaultAppEnvironmentsProvider
                {
                    ContentFormatter = DefaultJsonContentFormatter.Current,
                    PathProvider = DefaultPathProvider.Current
                });
            }
            set => _current = value;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            if (_activeAppEnvironment != null)
                return _activeAppEnvironment;

            string environmentsAsJson = File.ReadAllText(PathProvider.MapPath("environments.json"));

            AppEnvironment[] allEnvironments = ContentFormatter.DeSerialize<AppEnvironment[]>(environmentsAsJson);

            AppEnvironment[] activeEnvironments = allEnvironments.Where(env => env.IsActive).ToArray();

            if (activeEnvironments.Length == 0)
                throw new InvalidOperationException("There is no active environment");

            if (activeEnvironments.Length > 1)
                throw new InvalidOperationException("There are more than one active environment");

            _activeAppEnvironment = activeEnvironments.Single();

            return _activeAppEnvironment;
        }

        public override string ToString()
        {
            return _activeAppEnvironment?.ToString() ?? base.ToString();
        }
    }
}
