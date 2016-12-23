using System.Collections.Generic;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientProxyGeneratorSolutionProjectsSelector
    {
        IList<Project> GetInvolveableProjects(Workspace workspace, Solution solution, IList<Project> projects,
            HtmlClientProxyGeneratorMapping htmlClientProxyGeneratorMapping);
    }
}
