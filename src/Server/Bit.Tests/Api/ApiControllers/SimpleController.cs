using Bit.OData.ODataControllers;

namespace Bit.Tests.Api.ApiControllers
{
    public class SimpleController : DtoController
    {
        [Function]
        public virtual int Sum(int n1, int n2)
        {
            return n1 + n2;
        }
    }
}
