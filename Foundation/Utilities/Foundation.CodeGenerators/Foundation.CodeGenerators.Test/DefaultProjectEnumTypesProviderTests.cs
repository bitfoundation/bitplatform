using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Foundation.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Test
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

public sealed class ParameterAttribute : System.Attribute
{
    public ParameterAttribute(string name, System.Type type, bool isOptional = false)
    {
    }
}

public class DtoWithEnumController : DtoController<DtoWithEnum>
{
    [ActionAttribute]
    [ParameterAttribute(""gender"", typeof(TestGender))]
    public virtual int GetDtoWithEnumsByGender()
    {
        return 1;
    }

    [ActionAttribute]
    [ParameterAttribute(""gender"", typeof(TestGender2))]
    public virtual int GetDtoWithEnumsByGender2()
    {
        return 1;
    }
}

";

            IProjectDtoControllersProvider dtoControllersProvider = new DefaultProjectDtoControllersProvider();

            IProjectEnumTypesProvider enumTypesProvider = new DefaultProjectEnumTypesProvider(dtoControllersProvider, new DefaultProjectDtosProvider(dtoControllersProvider));

            Project proj = CreateProjectFromSourceCodes(sourceCodes);

            IList<EnumType> result = enumTypesProvider.GetProjectEnumTypes(proj, new[] { proj });

            Assert.IsTrue(result.Select(enumType => enumType.EnumTypeSymbol.Name).SequenceEqual(new[] { "TestGender", "TestGender2" }));

            Assert.IsTrue(result.First().Members.SequenceEqual(new[]
            {
                new EnumMember { Name = "Man", Value = 3 },
                new EnumMember { Name = "Woman", Value = 12 },
                new EnumMember { Name = "Other", Value = 13 }
            }, new EnumMemberEqualityComparer()));
        }

        private class EnumMemberEqualityComparer : IEqualityComparer<EnumMember>
        {
            public bool Equals(EnumMember x, EnumMember y)
            {
                return x.Name == y.Name && x.Value == y.Value;
            }

            public int GetHashCode(EnumMember obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
