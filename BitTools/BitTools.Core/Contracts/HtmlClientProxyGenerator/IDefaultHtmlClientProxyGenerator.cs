using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts.HtmlClientProxyGenerator
{
    public interface IDefaultHtmlClientProxyGenerator
    {
        Task GenerateCodes(Workspace workspace, string solutionFilePath, IList<string> projectNames);
    }
}