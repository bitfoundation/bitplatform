using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Contracts
{
    public interface IDtoRulesValidator
    {
        void Validate(DtoRules dtoRules);
    }
}
