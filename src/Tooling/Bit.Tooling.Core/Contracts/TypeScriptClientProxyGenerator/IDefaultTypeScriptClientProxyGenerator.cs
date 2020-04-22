using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator
{
    public interface IDefaultTypeScriptClientProxyGenerator
    {
        Task GenerateCodes(Workspace workspace);
    }
}