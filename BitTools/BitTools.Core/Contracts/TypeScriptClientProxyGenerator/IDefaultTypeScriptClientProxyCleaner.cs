using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts.TypeScriptClientProxyGenerator
{
    public interface IDefaultTypeScriptClientProxyCleaner
    {
        Task DeleteCodes(Workspace workspace);
    }
}