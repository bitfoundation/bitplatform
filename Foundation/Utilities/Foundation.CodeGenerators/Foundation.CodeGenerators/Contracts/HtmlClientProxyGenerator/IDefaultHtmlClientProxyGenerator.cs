using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IDefaultHtmlClientProxyGenerator
    {
        void GenerateCodes(Workspace worksapce, Solution solution, IList<Project> projects);
    }
}