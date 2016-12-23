using System.Collections.Generic;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientProxyGeneratorMappingsProvider
    {
        IList<HtmlClientProxyGeneratorMapping> GetHtmlClientProxyGeneratorMappings(Workspace workspace, Solution solution, IList<Project> projects);
    }
}
