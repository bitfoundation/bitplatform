using Bit.Tooling.Core.Model;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator
{
    public interface ICSharpClientMetadataGenerator
    {
        string GenerateMetadata(IList<Dto> dtos, IList<EnumType> enumTypes, IList<DtoController> controllers, BitCodeGeneratorMapping mapping);
    }
}
