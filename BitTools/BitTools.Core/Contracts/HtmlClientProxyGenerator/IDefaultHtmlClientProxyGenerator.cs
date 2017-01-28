using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts.HtmlClientProxyGenerator
{
    public interface IDefaultHtmlClientProxyGenerator
    {
        void GenerateCodes(Workspace workspace, Solution solution, IList<Project> projects);
    }
}