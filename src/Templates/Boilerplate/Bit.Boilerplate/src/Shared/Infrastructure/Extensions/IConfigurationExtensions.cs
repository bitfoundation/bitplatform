namespace Microsoft.Extensions.Configuration;

/// <summary>
/// The Bind method's performance gets improved by `EnableConfigurationBindingGenerator`, which
/// sometimes results into build error because of Obsolote members on Options class.
/// This extension class provides a workaround to use reflection to call the Bind method instead of using the generated code.
/// </summary>
public static partial class IConfigurationExtensions
{
    private static readonly Action<IConfiguration, object?> bind =
        typeof(ConfigurationBinder).GetMethod(nameof(ConfigurationBinder.Bind), [typeof(IConfiguration), typeof(object)])!
            .CreateDelegate<Action<IConfiguration, object?>>();

    extension(IConfiguration configuration)
    {
        /// <summary>
        /// <inheritdoc cref="IConfigurationExtensions"/>
        /// </summary>
        public IConfiguration DynamicBind<T>(T options)
        {
            bind(configuration, options);

            return configuration;
        }

        /// <summary>
        /// <inheritdoc cref="IConfigurationExtensions"/>
        /// </summary>
        public IConfiguration DynamicBind<T>(string sectionName, T options)
        {
            return DynamicBind(configuration.GetSection(sectionName), options);
        }

        /// <summary>
        /// <inheritdoc cref="IConfigurationExtensions"/>
        /// </summary>
        public T DynamicBind<T>()
            where T : class, new()
        {
            var instance = new T();
            DynamicBind(configuration, instance);
            return instance;
        }
    }
}
