using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.OData;
using Microsoft.Owin;
using System.Reflection;

namespace Bit.Api.Middlewares.WebApi.OData
{
    public class DefaultODataActionPayloadDeserializer : ODataActionPayloadDeserializer
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        protected DefaultODataActionPayloadDeserializer()
            : base(null)
        {

        }

        public DefaultODataActionPayloadDeserializer(System.Web.Http.Dependencies.IDependencyResolver dependencyResolver,
            ODataDeserializerProvider deserializerProvider)
            : base(deserializerProvider)
        {
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));

            _stringCorrectors = (IEnumerable<IStringCorrector>)dependencyResolver.GetServices(typeof(IStringCorrector).GetTypeInfo());
        }

        public override object Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)
        {
            object result = base.Read(messageReader, type, readContext);
            if (result is ODataActionParameters)
            {
                ODataActionParameters parameters = result as ODataActionParameters;
                foreach (var parameter in parameters.Select(p => new { p.Key, p.Value }).ToList())
                {
                    if (parameter.Value != null)
                    {
                        if (parameter.Value is DateTimeOffset)
                        {
                            IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                                    .GetDependencyResolver();

                            ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

                            DateTimeOffset date = timeZoneManager.MapFromClientToServer(((DateTimeOffset)parameter.Value));

                            parameters[parameter.Key] = date;
                        }
                        else if (parameter.Value is IEnumerable<DateTimeOffset>)
                        {
                            IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                                                                .GetDependencyResolver();

                            ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

                            IEnumerable<DateTimeOffset> dates = (IEnumerable<DateTimeOffset>)parameter.Value;

                            parameters[parameter.Key] = FixIEnumerableOfDateTimes(dates, timeZoneManager);
                        }
                        else if (parameter.Value is string)
                        {
                            string initialString = parameter.Value.ToString();

                            foreach (IStringCorrector stringCorrector in _stringCorrectors)
                            {
                                initialString = stringCorrector.CorrectString(initialString);
                            }

                            parameters[parameter.Key] = initialString;
                        }
                        else if (parameter.Value is IEnumerable<string>)
                        {
                            parameters[parameter.Key] = FixIEnumerableOfStrings((IEnumerable<string>)parameter.Value);
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private IEnumerable<DateTimeOffset> FixIEnumerableOfDateTimes(IEnumerable<DateTimeOffset> dates, ITimeZoneManager timeZoneManager)
        {
            foreach (DateTimeOffset date in dates)
            {
                yield return timeZoneManager.MapFromClientToServer(date);
            }
        }

        private IEnumerable<string> FixIEnumerableOfStrings(IEnumerable<string> values)
        {
            foreach (string str in values)
            {
                string newStr = str;
                foreach (IStringCorrector stringCorrector in _stringCorrectors)
                {
                    newStr = stringCorrector.CorrectString(newStr);
                }
                yield return newStr;
            }
        }
    }
}