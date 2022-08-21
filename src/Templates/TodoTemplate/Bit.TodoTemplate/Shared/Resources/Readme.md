You don't have to use AppStrings.resx for entire your application!
You can create custom resx files in Shared, Web or even Api project (Like EmailStrings.resx) and then you can inject IStringLocalizer<EmailStrings> or whatever in your razor pages and api controllers.
You can also use separated resx files for each dto or group of dto classes.
In order to make resx files work for dto classes, customize StringLocalizerProvider's ProvideLocalizer method as followings:

```cs
public static IStringLocalizer ProvideLocalizer(Type dtoType, IStringLocalizerFactory factory)
{
    if (dtoType == typeof(ProductDto) || dtoType == typeof(CategoryDto))
    {
        return factory.Create(typeof(ProductCatalogStrings)); // We've created ProductCatalogStrings for both ProductDto & CategoryDto
    }
    return factory.Create(typeof(AppStrings));
}
```

In order to enable multilingual, set MultilingualEnabled to true in Directory.Build.props