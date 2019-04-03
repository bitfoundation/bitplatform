using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitCodeGenerator.Implementations
{
    public class DefaultBitCodeGeneratorOrderedProjectsProvider : IBitCodeGeneratorOrderedProjectsProvider
    {
        public virtual IList<Project> GetInvolveableProjects(Workspace workspace, IList<Project> projects, BitCodeGeneratorMapping bitCodeGeneratorMapping)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            if (bitCodeGeneratorMapping == null)
                throw new ArgumentNullException(nameof(bitCodeGeneratorMapping));

            return bitCodeGeneratorMapping.SourceProjects
                .Select(projInfo => projects.ExtendedSingleOrDefault($"Looking for {projInfo.Name} in [ {string.Join(",", bitCodeGeneratorMapping.SourceProjects.Select(p => p.Name))} ]", p => p.Name == projInfo.Name && p.Language == LanguageNames.CSharp))
                .Where(p => p != null)
                .ToList();
        }
    }
}
