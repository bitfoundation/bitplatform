using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyVersionTracking : IVersionTracking
    {
        const string versionsKey = "VersionTracking.Versions";
        const string buildsKey = "VersionTracking.Builds";

        readonly string sharedName = "Versiontracking";

        readonly Dictionary<string, List<string>> versionTrail;
        readonly IPreferences _preferences;
        readonly IAppInfo _appInfo;

        public WebAssemblyVersionTracking(IPreferences preferences, IAppInfo appInfo)
        {
            _preferences = preferences;
            _appInfo = appInfo;

            IsFirstLaunchEver = !_preferences.ContainsKey(versionsKey, sharedName) || !_preferences.ContainsKey(buildsKey, sharedName);
            if (IsFirstLaunchEver)
            {
                versionTrail = new Dictionary<string, List<string>>
                {
                    { versionsKey, new List<string>() },
                    { buildsKey, new List<string>() }
                };
            }
            else
            {
                versionTrail = new Dictionary<string, List<string>>
                {
                    { versionsKey, ReadHistory(versionsKey).ToList() },
                    { buildsKey, ReadHistory(buildsKey).ToList() }
                };
            }

            IsFirstLaunchForCurrentVersion = !versionTrail[versionsKey].Contains(CurrentVersion);
            if (IsFirstLaunchForCurrentVersion)
            {
                versionTrail[versionsKey].Add(CurrentVersion);
            }

            IsFirstLaunchForCurrentBuild = !versionTrail[buildsKey].Contains(CurrentBuild);
            if (IsFirstLaunchForCurrentBuild)
            {
                versionTrail[buildsKey].Add(CurrentBuild);
            }

            if (IsFirstLaunchForCurrentVersion || IsFirstLaunchForCurrentBuild)
            {
                WriteHistory(versionsKey, versionTrail[versionsKey]);
                WriteHistory(buildsKey, versionTrail[buildsKey]);
            }
        }

        public void Track()
        {
        }

        public bool IsFirstLaunchEver { get; private set; }

        public bool IsFirstLaunchForCurrentVersion { get; private set; }

        public bool IsFirstLaunchForCurrentBuild { get; private set; }

        public string CurrentVersion => _appInfo.VersionString;

        public string CurrentBuild => _appInfo.BuildString;

        public string PreviousVersion => GetPrevious(versionsKey);

        public string PreviousBuild => GetPrevious(buildsKey);

        public string FirstInstalledVersion => versionTrail[versionsKey].FirstOrDefault();

        public string FirstInstalledBuild => versionTrail[buildsKey].FirstOrDefault();

        public IEnumerable<string> VersionHistory => versionTrail[versionsKey].ToArray();

        public IEnumerable<string> BuildHistory => versionTrail[buildsKey].ToArray();

        public bool IsFirstLaunchForVersion(string version)
            => CurrentVersion == version && IsFirstLaunchForCurrentVersion;

        public bool IsFirstLaunchForBuild(string build)
            => CurrentBuild == build && IsFirstLaunchForCurrentBuild;

        internal string GetStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("VersionTracking");
            sb.AppendLine($"  IsFirstLaunchEver:              {IsFirstLaunchEver}");
            sb.AppendLine($"  IsFirstLaunchForCurrentVersion: {IsFirstLaunchForCurrentVersion}");
            sb.AppendLine($"  IsFirstLaunchForCurrentBuild:   {IsFirstLaunchForCurrentBuild}");
            sb.AppendLine();
            sb.AppendLine($"  CurrentVersion:                 {CurrentVersion}");
            sb.AppendLine($"  PreviousVersion:                {PreviousVersion}");
            sb.AppendLine($"  FirstInstalledVersion:          {FirstInstalledVersion}");
            sb.AppendLine($"  VersionHistory:                 [{string.Join(", ", VersionHistory)}]");
            sb.AppendLine();
            sb.AppendLine($"  CurrentBuild:                   {CurrentBuild}");
            sb.AppendLine($"  PreviousBuild:                  {PreviousBuild}");
            sb.AppendLine($"  FirstInstalledBuild:            {FirstInstalledBuild}");
            sb.AppendLine($"  BuildHistory:                   [{string.Join(", ", BuildHistory)}]");
            return sb.ToString();
        }

        string[] ReadHistory(string key)
            => _preferences.Get(key, null, sharedName)?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

        void WriteHistory(string key, IEnumerable<string> history)
            => _preferences.Set(key, string.Join("|", history), sharedName);

        string GetPrevious(string key)
        {
            var trail = versionTrail[key];
            return (trail.Count >= 2) ? trail[trail.Count - 2] : null;
        }
    }
}
