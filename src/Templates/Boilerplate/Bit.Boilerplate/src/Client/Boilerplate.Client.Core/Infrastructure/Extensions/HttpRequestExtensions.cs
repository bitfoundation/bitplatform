namespace System.Net.Http;

public static class HttpRequestExtensions
{
    private static readonly ConcurrentDictionary<(Type ControllerType, string ActionName, string ParameterTypes, Type AttributeType), bool> apiAttributes = new();

    extension(HttpRequestMessage request)
    {
        /// <summary>
        /// <inheritdoc cref="AuthorizedApiAttribute"/>
        /// </summary>
        public bool HasAuthorizedApiAttribute()
        {
            return request.HasApiAttribute<AuthorizedApiAttribute>();
        }

        /// <summary>
        /// <see cref="NoRetryPolicyAttribute"/>
        /// </summary>
        public bool HasNoRetryPolicyAttribute()
        {
            return request.HasApiAttribute<NoRetryPolicyAttribute>();
        }

        /// <summary>
        /// <see cref="ExternalApiAttribute"/>
        /// </summary>
        public bool HasExternalApiAttribute()
        {
            return request.HasApiAttribute<ExternalApiAttribute>();
        }

        private bool HasApiAttribute<TApiAttribute>()
            where TApiAttribute : Attribute
        {
            if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
                return false;

            var actionName = (string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!;
            var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();

            return apiAttributes.GetOrAdd((controllerType!, actionName, string.Join(',', parameterTypes.Select(t => t.FullName)), typeof(TApiAttribute)), _ =>
            {
                var method = controllerType!.GetMethod(actionName, parameterTypes)!;

                return controllerType.GetCustomAttribute<TApiAttribute>(inherit: true) is not null ||
                       method.GetCustomAttribute<TApiAttribute>() is not null;
            });
        }
    }
}
