using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts.HtmlClientProxyGenerator
{
    public interface IDefaultHtmlClientProxyCleaner
    {
        void DeleteCodes(Workspace workspace, Solution solution, IList<Project> projects);
    }
}