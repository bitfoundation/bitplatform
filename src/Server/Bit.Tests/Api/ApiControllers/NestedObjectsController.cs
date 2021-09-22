using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers
{
    public class NestedObjectsController : DtoController<NestedDto>
    {
        [Function]
        public virtual async Task<NestedComplex3[]> GetComplexObjects()
        {
            return null;
        }

        [Function]
        public virtual async Task<NestedComplex3[]> GetComplexObjects2()
        {
            return new[] 
            {
                new NestedComplex3 { Name = "A" },
                new NestedComplex3 { Name = "A" },
                new NestedComplex3 { Name = "A" },
                new NestedComplex3 { Name = "B" },
                new NestedComplex3 { Name = "B" }
            };
        }

        public class SomeActionArgs
        {
            public NestedComplex3 Test4 { get; set; }
            public string Test { get; set; }
        }

        [Action]
        public virtual string SomeAction(SomeActionArgs args)
        {
            return args.Test4.Obj4.Test.ToString();
        }
    }

    public class NestedDto : IDto
    {
        [Key]
        public virtual int EntityId { get; set; }

        public virtual NestedComplex ComplexObj { get; set; }

        public virtual NestedComplex2 ComplexObj2 { get; set; }
    }

    [ComplexType]
    public class NestedComplex
    {
        public virtual string Name { get; set; }
    }

    [ComplexType]
    public class NestedComplex2
    {
        public virtual string Name { get; set; }
    }

    [ComplexType]
    public class NestedComplex3
    {
        public virtual string Name { get; set; }

        public virtual NestedComplex4 Obj4 { get; set; }
    }

    [ComplexType]
    public class NestedComplex4
    {
        public virtual string Name { get; set; }

        public virtual NestedEnum Test { get; set; }
    }

    [ComplexType]
    public class NestedComplex5
    {
        public virtual string Name { get; set; }

        public virtual NestedEnum2 Test2 { get; set; }
    }

    public enum NestedEnum
    {
        B,
        A
    }

    public enum NestedEnum2
    {
        A,
        B
    }
}
