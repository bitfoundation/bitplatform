using System;
using System.Collections.Generic;
using System.Linq;
using BitTools.Core.Contracts.HtmlClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using BitTools.Core.Contracts;

namespace BitCodeGenerator.Implementations
{
    public class DefaultBitCodeGeneratorOrderedProjectsProvider : IBitCodeGeneratorOrderedProjectsProvider
    {
        public virtual IList<Project> GetInvolveableProjects(Workspace workspace, Solution solution, IList<Project> projects, BitCodeGeneratorMapping htmlClientProxyGeneratorMapping)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            if (htmlClientProxyGeneratorMapping == null)
                throw new ArgumentNullException(nameof(htmlClientProxyGeneratorMapping));

            return htmlClientProxyGeneratorMapping.SourceProjects
                .Select(projInfo => projects.SingleOrDefault(p => p.Name == projInfo.Name))
                .Where(p => p != null)
                .ToList();
        }
    }
}
