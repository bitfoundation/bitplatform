using Microsoft.CodeAnalysis;
using System;
using System.IO;

namespace Bit.Tooling.Core.Model
{
    public class BitConfig
    {
        public virtual BitCodeGeneratorConfig BitCodeGeneratorConfigs { get; set; } = default!;
        public virtual string? TargetFramework { get; set; }
        public virtual string? VisualStudioBuildToolsVersion { get; set; } // 5.0.100 for example
    }

    public class BitCodeGeneratorConfig
    {
        public virtual BitCodeGeneratorMapping[] BitCodeGeneratorMappings { get; set; } = default!;
    }

    public enum GenerationType
    {
        CSharpSimpleODataClient, 
        CSharpHttpClient,
        TypeScriptJayDataClient
    }

    public class BitCodeGeneratorMapping
    {
        public virtual string Route { get; set; } = default!;

        [Obsolete("Bit 1.3.80 and above are using default odata namespace. There is no need to customize this anymore.")]
        public virtual string Namespace { get; set; } = default!;

        public virtual ProjectInfo[] SourceProjects { get; set; } = default!;

        public virtual ProjectInfo DestinationProject { get; set; } = default!;

        public virtual NamespaceAlias[] NamespaceAliases { get; set; } = Array.Empty<NamespaceAlias>();

        public virtual string DestinationFileName { get; set; } = default!;

        public virtual string TypingsPath { get; set; } = default!;

        private GenerationType? _GenerationType;

        public virtual GenerationType GenerationType
        {
            get => _GenerationType is not null ? _GenerationType.Value : (string.IsNullOrEmpty(TypingsPath) ? GenerationType.CSharpSimpleODataClient : GenerationType.TypeScriptJayDataClient);
            set => _GenerationType = value;
        }

        public override string ToString()
        {
            return Route;
        }
    }

    public class ProjectInfo
    {
        public virtual string Name { get; set; } = default!;

        public static bool operator ==(ProjectInfo? pi, Project? p)
        {
            if (p is null && pi is null)
                return true;

            if (p is null)
                return false;

            if (pi is null)
                return false;

            return p.Name == pi.Name || Path.GetFileNameWithoutExtension(p.FilePath) == pi.Name;
        }

        public static bool operator !=(ProjectInfo pi, Project p)
        {
            if (p is null && pi is null)
                return false;

            if (p is null)
                return true;

            if (pi is null)
                return true;

            return p.Name != pi.Name || Path.GetFileNameWithoutExtension(p.FilePath) != pi.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class NamespaceAlias
    {
        public virtual string Namespace { get; set; } = default!;

        public virtual string Alias { get; set; } = default!;

        public override string ToString()
        {
            return Namespace;
        }
    }
}
