using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator
{
    public interface IDefaultCSharpClientProxyGenerator
    {
        Task GenerateCodes(Workspace workspace);
    }
}