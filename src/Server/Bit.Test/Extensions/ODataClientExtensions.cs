using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using System;
using System.Reflection;

namespace Simple.OData.Client
{
    public static class ODataClientExtensions
    {
        public static IBoundClient<TDto> Controller<TController, TDto>(this IODataClient client)
            where TController : DtoController<TDto>
            where TDto : class, IDto
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.For<TDto>(typeof(TController).GetTypeInfo().Name.Replace("Controller", string.Empty));
        }
    }
}
