using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.OData.Formatter.Deserialization;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Owin;
using System.Reflection;
using System.Linq;

namespace Bit.Api.Middlewares.WebApi.OData
{
    public class DefaultODataResourceDeserializer : ODataResourceDeserializer
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        protected DefaultODataResourceDeserializer()
            : base(null)
        {

        }

        public DefaultODataResourceDeserializer(System.Web.Http.Dependencies.IDependencyResolver dependencyResolver,
                    ODataDeserializerProvider deserializerProvider)
            : base(deserializerProvider)
        {
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));

            _stringCorrectors = dependencyResolver.GetServices(typeof(IStringCorrector).GetTypeInfo()).Cast<IStringCorrector>().ToArray();
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
            catch (ODataException ex) when (ex.Message == "Does not support untyped value in non-open type.")
            {

            }
        }
    }
}