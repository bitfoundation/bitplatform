using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IDefaultHtmlClientProxyCleaner
    {
        void DeleteCodes(Workspace workspace, Solution solution, IList<Project> projects);
    }
}