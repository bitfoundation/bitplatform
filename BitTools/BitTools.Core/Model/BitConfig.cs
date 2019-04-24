using Microsoft.CodeAnalysis;
using System;
using System.IO;

namespace BitTools.Core.Model
{
    public class BitConfig
    {
        public virtual BitCodeGeneratorConfig BitCodeGeneratorConfigs { get; set; }
    }

    public class BitCodeGeneratorConfig
    {
        public virtual BitCodeGeneratorMapping[] BitCodeGeneratorMappings { get; set; }
    }

    public enum GenerationType
    {
        CSharp, TypeScript
    }

    public class BitCodeGeneratorMapping
    {
        public virtual string Route { get; set; }

        [Obsolete("Bit 1.3.80 and above are using default odata namespace. There is no need to customize this anymore.")]
        public virtual string Namespace { get; set; }

        public virtual ProjectInfo[] SourceProjects { get; set; }

        public virtual ProjectInfo DestinationProject { get; set; }

        public virtual NamespaceAlias[] NamespaceAliases { get; set; } = new NamespaceAlias[] { };

        public virtual string DestinationFileName { get; set; }

        public virtual string TypingsPath { get; set; }

        public virtual GenerationType GenerationType => string.IsNullOrEmpty(TypingsPath) ? GenerationType.CSharp : GenerationType.TypeScript;
    }

    public class ProjectInfo
    {
        public virtual string Name { get; set; }

        public bool IsThisProject(Project p)
        {
            return p.Name == Name || Path.GetFileNameWithoutExtension(p.FilePath) == Name;
        }
    }

    public class NamespaceAlias
    {
        public virtual string Namespace { get; set; }

        public virtual string Alias { get; set; }
    }
}
