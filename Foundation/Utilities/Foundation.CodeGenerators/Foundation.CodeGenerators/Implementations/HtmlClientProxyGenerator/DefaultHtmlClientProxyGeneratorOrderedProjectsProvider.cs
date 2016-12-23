using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyGeneratorSolutionProjectsSelector : IHtmlClientProxyGeneratorSolutionProjectsSelector
    {
        public virtual IList<Project> GetInvolveableProjects(Workspace workspace, Solution solution, IList<Project> projects, HtmlClientProxyGeneratorMapping htmlClientProxyGeneratorMapping)
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
