using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts.TypeScriptClientProxyGenerator
{
    public interface IDefaultTypeScriptClientProxyGenerator
    {
        Task GenerateCodes(Workspace workspace);
    }
}