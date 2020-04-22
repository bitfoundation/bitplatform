using BitCodeGenerator.Implementations;
using BitCodeGenerator.Test.Helpers;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.Implementations
{
    [TestClass]
    public class DefaultProjectEnumTypesProviderTests : CodeGeneratorTest
    {
        [TestMethod]
        public virtual async Task DefaultEnumTypesProviderShouldReturnEnumTypes()
        {
            const string sourceCodes = @"

public interface IDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class DtoWithEnum : IDto
{
    public virtual int Id { get; set; }

    public virtual TestGender? Gender { get; set; }

    public virtual string Test { get; set; }
}

public enum TestGender
{
    Man = 3, Woman = 12, Other
}

public enum TestGender2
{
    Man = 3, Woman = 12, Other
}

public class DtoWithEnumController : DtoController<DtoWithEnum>
{
    public class GetDtoWithEnumsByGenderParameters
    {
        public virtual TestGender gender { get; set; }
    }

    [ActionAttribute]
    public virtual int GetDtoWithEnumsByGender(GetDtoWithEnumsByGenderParameters parameters)
    {
        return 1;
    }

    public class GetDtoWithEnumsByGender2Parameters
    {
        public virtual TestGender2 gender { get; set; }
    }

    [ActionAttribute]
    public virtual int GetDtoWithEnumsByGender2(GetDtoWithEnumsByGender2Parameters parameters)
    {
        return 1;
    }
}

";

            IProjectDtoControllersProvider projectDtoControllersProvider = new DefaultProjectDtoControllersProvider();

            IProjectEnumTypesProvider projectEnumTypesProvider = new DefaultProjectEnumTypesProvider(projectDtoControllersProvider, new DefaultProjectDtosProvider(projectDtoControllersProvider));

            Project proj = CreateProjectFromSourceCodes(sourceCodes);

            IList<EnumType> result = await projectEnumTypesProvider.GetProjectEnumTypes(proj, new[] { proj });

            Assert.IsTrue(result.Select(enumType => enumType.EnumTypeSymbol.Name).SequenceEqual(new[] { "TestGender", "TestGender2" }));

            Assert.IsTrue(result.First().Members.SequenceEqual(new[]
            {
                new EnumMember { Name = "Man", Value = 3,Index = 0},
                new EnumMember { Name = "Woman", Value = 12,Index = 1},
                new EnumMember { Name = "Other", Value = 13,Index = 2}
            }, new EnumMemberEqualityComparer()));
        }

        private class EnumMemberEqualityComparer : IEqualityComparer<EnumMember>
        {
            public bool Equals(EnumMember x, EnumMember y)
            {
                return x?.Name == y?.Name && x?.Value == y?.Value && x?.Index == y?.Index;
            }

            public int GetHashCode(EnumMember obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
