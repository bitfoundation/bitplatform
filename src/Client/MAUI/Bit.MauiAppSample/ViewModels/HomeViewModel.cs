using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bit.Model.Contracts;

namespace Bit.MauiAppSample.ViewModels
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

    public class HomeViewModel : SampleViewModelBase
    {
        public async override Task OnInitializedAsync()
        {
            var result = await ODataClient.For("ParentEntities").FindEntriesAsync();
            var result2 = await ODataClient.For<TestComplexDto>("TestComplex")
                .FindEntriesAsync();

            await base.OnInitializedAsync();
        }
    }
}
