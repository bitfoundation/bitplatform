using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataEnumDeserializer : ODataEnumDeserializer
    {
        public override object ReadInline(object item, IEdmTypeReference edmType, ODataDeserializerContext readContext)
        {
            ODataEnumValue valueItem = item as ODataEnumValue;
            if (valueItem != null && !string.IsNullOrEmpty(valueItem.Value) && valueItem.Value.Contains("'"))
                valueItem = new ODataEnumValue(valueItem.Value.Replace("'", ""));
            object result = base.ReadInline(valueItem, edmType, readContext);
            return result;
        }
    }
}
