namespace Bit.BlazorUI;

public class ElementStyleBuilder : ElementAttributeBuilder
{
    protected override char Separator { get => ';'; }
}

public class ElementClassBuilder : ElementAttributeBuilder
{
    protected override char Separator { get => ' '; }
}

public abstract class ElementAttributeBuilder
{
    private bool _dirty = true;
    private string? _value;
    private List<Func<string?>> _registrars = new();
    private List<Func<Action<string?>, string?>> _actionedRegistrars = new();

    protected abstract char Separator { get; }

    public string? Value
    {
        get
        {
            if (_dirty)
            {
                Build();
            }
            return _value;
        }
    }

    public ElementAttributeBuilder Register(Func<string?> registrar)
    {
        _registrars.Add(registrar);
        return this;
    }

    public ElementAttributeBuilder Register(Func<Action<string?>, string?> actionedRegistrar)
    {
        _actionedRegistrars.Add(actionedRegistrar);
        return this;
    }

    public void Reset()
    {
        _dirty = true;
    }

    private void Build()
    {
        var values = new List<string?>();

        var values1 = _registrars.Select(r => r());
        var values2 = _actionedRegistrars.Select(ar => ar(values.Add)).ToArray();

        var value = string.Join(Separator, values.Concat(values1).Concat(values2).Where(s => s.HasValue()));

        _value = value.HasValue() ? value : null;
        _dirty = false;
    }
}
