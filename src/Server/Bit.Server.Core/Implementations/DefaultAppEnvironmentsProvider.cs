using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.IO;
using System.Linq;

namespace Bit.Core.Implementations
{
    public class DefaultAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        private AppEnvironment? _activeAppEnvironment;
        private static IAppEnvironmentsProvider _current = default!;

        public virtual IPathProvider PathProvider { get; set; } = default!;
        public virtual IContentFormatter ContentFormatter { get; set; } = default!;

        public static IAppEnvironmentsProvider Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new DefaultAppEnvironmentsProvider
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
            var (success, message) = TryGetActiveAppEnvironment(out AppEnvironment? activeAppEnvironment);
            if (success == true && activeAppEnvironment != null)
                return activeAppEnvironment;
            throw new InvalidOperationException(message);
        }

        public virtual (bool success, string? message) TryGetActiveAppEnvironment(out AppEnvironment? activeAppEnvironment)
        {
            activeAppEnvironment = null;

            if (_activeAppEnvironment != null)
            {
                activeAppEnvironment = _activeAppEnvironment;
                return (true, null);
            }

            string path = PathProvider.MapPath("environments.json");

            if (File.Exists(path))
            {
                string environmentsAsJson = File.ReadAllText(PathProvider.MapPath("environments.json"));

                AppEnvironment[] allEnvironments = ContentFormatter.Deserialize<AppEnvironment[]>(environmentsAsJson);

                AppEnvironment[] activeEnvironments = allEnvironments.Where(env => env.IsActive).ToArray();

                if (!activeEnvironments.Any())
                    return (false, "There is no active environment");

                if (activeEnvironments.Length > 1)
                    return (false, "There are more than one active environment");

                activeAppEnvironment = _activeAppEnvironment = activeEnvironments.Single();

                return (true, null);
            }
            else
            {
                return (false, $"environments.json could not be found at {path}");
            }
        }

        public override string? ToString()
        {
            return _activeAppEnvironment?.ToString() ?? base.ToString();
        }
    }
}