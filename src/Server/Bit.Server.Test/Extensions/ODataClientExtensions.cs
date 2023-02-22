using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using System;
using System.Reflection;

namespace Simple.OData.Client
{
    public static class ODataClientExtensions
    {
        public static IBoundClient<TDto> Controller<TController, TDto>(this IODataClient client)
            where TDto : class
            where TController : DtoController<TDto>
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.For<TDto>(typeof(TController).GetTypeInfo().Name.Replace("Controller", string.Empty, StringComparison.InvariantCulture));
        }
    }
}
