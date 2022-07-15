using System.Collections.Concurrent;
using System.Reflection;

namespace Bit.BlazorUI;

/* We favour using a non-generic design here because the "entry point" where this class is used
 * is a JsonConverter that has to work for all types of ObjectEnum. Therefore the converter isn't
 * generic and we don't have a generic parameter to begin with. We would need to use reflection
 * to create the factory and if we just work with Type, we can reduce the reflection use a bit.
 */
internal class ObjectEnumFactory
{
    private static readonly ConcurrentDictionary<Type, ObjectEnumFactory> _factorySingletons = new ConcurrentDictionary<Type, ObjectEnumFactory>();

    private readonly Dictionary<Type, ConstructorInfo> _constructorCache;
    private readonly Type _enumType;

    /// <summary>
    /// Gets (and creates if needed) the singleton-factory for this <paramref name="enumType"/>.
    /// </summary>
    /// <param name="enumType">The <see cref="ObjectEnum"/>-type whose factory to get.</param>
    public static ObjectEnumFactory GetFactory(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));

        if (!typeof(ObjectEnum).IsAssignableFrom(enumType))
            throw new ArgumentException($"The type '{enumType.FullName}' doesn't inherit from '{typeof(ObjectEnum).FullName}'");

        return _factorySingletons.GetOrAdd(enumType, type => new ObjectEnumFactory(type));
    }

    private ObjectEnumFactory(Type enumType)
    {
        // checks omitted because the constructor is non-public
        _enumType = enumType;
        _constructorCache = CreateConstructorDictionary();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ObjectEnum"/>-type this factory is for.
    /// If there is no suitable constructor for the <paramref name="value"/>, a
    /// <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <param name="value">The value used for instantiating the <see cref="ObjectEnum"/>.</param>
    public ObjectEnum Create(object value) => Create(value, value.GetType());

    /// <summary>
    /// Creates a new instance of the <see cref="ObjectEnum"/>-type this factory is for.
    /// If there is no suitable constructor for the type <paramref name="valueType"/>, a
    /// <see cref="NotSupportedException"/> will be thrown.
    /// <para>
    /// Use this method if the type of <paramref name="value"/> is already known
    /// (and you're sure about it).
    /// </para>
    /// </summary>
    /// <param name="value">The value used for instantiating the <see cref="ObjectEnum"/>.</param>
    /// <param name="valueType">The <see cref="Type"/> of <paramref name="value"/>.</param>
    public ObjectEnum Create(object value, Type valueType)
    {
        if (_constructorCache.TryGetValue(valueType, out ConstructorInfo constructor))
        {
            return (ObjectEnum)constructor.Invoke(new[] { value });
        }

        if (ObjectEnum.IsSupportedSerializationType(valueType))
        {
            throw new NotSupportedException($"The object enum '{_enumType.FullName}' doesn't have a constructor which takes a single " +
                                            $"argument of type '{valueType.FullName}'.");
        }
        else
        {
            throw new NotSupportedException($"The type '{valueType}' isn't supported for serialization within {nameof(ObjectEnum)}.");
        }
    }

    /// <summary>
    /// Checks if a suitable constructor for this <paramref name="contentType"/> exists which
    /// can be used to create a new instance of that <see cref="ObjectEnum"/>-type.
    /// </summary>
    /// <param name="contentType">The <see cref="Type"/> of the enum-content to look for.</param>
    /// <returns><see langword="true"/> if there is a suitable constructor for that <paramref name="contentType"/>;
    /// otherwise <see langword="false"/>.</returns>
    public bool CanConvertFrom(Type contentType) => _constructorCache.ContainsKey(contentType);

    private Dictionary<Type, ConstructorInfo> CreateConstructorDictionary()
    {
        Dictionary<Type, ConstructorInfo> dict = new Dictionary<Type, ConstructorInfo>();
        ConstructorInfo[] constructors = _enumType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] constructorParams = constructor.GetParameters();
            if (constructorParams.Length != 1)
            {
                continue;
            }

            Type paramType = constructorParams[0].ParameterType;
            if (ObjectEnum.IsSupportedSerializationType(paramType))
            {
                dict.Add(paramType, constructor);
            }
        }

        if (dict.Count == 0)
        {
            throw new NotSupportedException($"The {nameof(ObjectEnum)} type '{_enumType.FullName}' doesn't have any " +
                                            $"suitable constructors for deserialization.");
        }

        return dict;
    }
}
