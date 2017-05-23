using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.OData.Formatter.Deserialization;
using Foundation.Core.Contracts;
using Microsoft.OData.Edm;
using Foundation.Api.Contracts;
using Microsoft.OData;
using Microsoft.Owin;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataResourceDeserializer : ODataResourceDeserializer
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        protected DefaultODataResourceDeserializer()
            : base(null)
        {

        }

        public DefaultODataResourceDeserializer(IEnumerable<IStringCorrector> stringCorrectors,
            ODataDeserializerProvider deserializerProvider)
            : base(deserializerProvider)
        {
            if (stringCorrectors == null)
                throw new ArgumentNullException(nameof(stringCorrectors));

            _stringCorrectors = stringCorrectors;
        }

        public override void ApplyStructuralProperty(object resource, ODataProperty structuralProperty, IEdmStructuredTypeReference structuredType, ODataDeserializerContext readContext)
        {
            if (structuralProperty?.Value != null)
            {
                if (structuralProperty.Value is DateTimeOffset)
                {
                    IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                                    .GetDependencyResolver();

                    ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

                    structuralProperty.Value = timeZoneManager.MapFromClientToServer(((DateTimeOffset)structuralProperty.Value));
                }
                else if (structuralProperty.Value is string)
                {
                    string rawString = structuralProperty.Value.ToString();
                    foreach (IStringCorrector stringCorrector in _stringCorrectors)
                    {
                        rawString = stringCorrector.CorrectString(rawString);
                    }
                    structuralProperty.Value = rawString;
                }
            }

            try
            {
                base.ApplyStructuralProperty(resource, structuralProperty, structuredType, readContext);
            }
            catch (ODataException ex)
            {
                if (ex.Message != "Does not support untyped value in non-open type.")
                    throw;
            }
        }
    }
}