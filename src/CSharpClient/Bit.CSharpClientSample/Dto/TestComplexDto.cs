using Bit.Model.Contracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bit.CSharpClientSample.Dto
{
    public class TestComplexDto : IDto
    {
        [Key]
        public virtual int EntityId { get; set; }

        public virtual ComplexObj ComplexObj { get; set; }
    }

    [ComplexType]
    public class ComplexObj
    {
        public virtual string Name { get; set; }
    }

    [ComplexType]
    public class ComplexObj2
    {
        public virtual string Name { get; set; }
    }
}
