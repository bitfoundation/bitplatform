using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts.CSharpClientProxyGenerator
{
    public interface IDefaultCSharpClientProxyGenerator
    {
        Task GenerateCodes(Workspace workspace);
    }
}