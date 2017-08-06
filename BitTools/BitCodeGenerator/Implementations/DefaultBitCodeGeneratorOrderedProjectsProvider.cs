using System;
using System.Collections.Generic;
using System.Linq;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using BitTools.Core.Contracts;

namespace BitCodeGenerator.Implementations
{
    public class DefaultBitCodeGeneratorOrderedProjectsProvider : IBitCodeGeneratorOrderedProjectsProvider
    {
        public virtual IList<Project> GetInvolveableProjects(Solution solution, IList<Project> projects, BitCodeGeneratorMapping htmlClientProxyGeneratorMapping)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            if (htmlClientProxyGeneratorMapping == null)
                throw new ArgumentNullException(nameof(htmlClientProxyGeneratorMapping));

            return htmlClientProxyGeneratorMapping.SourceProjects
                .Select(projInfo => projects.ExtendedSingleOrDefault($"Looking for {projInfo.Name} in [ {string.Join(",", htmlClientProxyGeneratorMapping.SourceProjects.Select(p => p.Name))} ]", p => p.Name == projInfo.Name && p.Language == LanguageNames.CSharp))
                .Where(p => p != null)
                .ToList();
        }
    }
}
