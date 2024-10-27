using System.Collections.Concurrent;

namespace Boilerplate.Tests.Services;

public static class StringLocalizerFactory
{
    private static readonly ConcurrentDictionary<(string, Type), IStringLocalizer> _stringLocalizerCache = new();

    public static IStringLocalizer<TResourceSource> Create<TResourceSource>(string culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(culture);

        var localizer = _stringLocalizerCache.GetOrAdd((culture, typeof(TResourceSource)),
            (_) => new CulturedStringLocalizer<TResourceSource>(culture));
        return (IStringLocalizer<TResourceSource>)localizer;
    }
}

public class CulturedStringLocalizer<TResourceSource>(string culture) : IStringLocalizer<TResourceSource>
{
    private readonly CultureInfo _cultureInfo = CultureInfoManager.CreateCultureInfo(culture);
    private readonly ResourceManager _resourceManager = new(typeof(TResourceSource));

    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var value = _resourceManager.GetString(name, _cultureInfo);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _resourceManager.BaseName);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var format = _resourceManager.GetString(name, _cultureInfo);
            var value = string.Format(_cultureInfo, format ?? name, arguments);

            return new LocalizedString(name, value, resourceNotFound: format == null, searchedLocation: _resourceManager.BaseName);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => throw new NotImplementedException();
}
