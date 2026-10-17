namespace Bit.BbScanner;

/// <summary>
/// A polynomial over a <see cref="BbModulusGF"/>, stored most significant coefficient first.
/// </summary>
public sealed class BbModulusPoly
{
    private readonly BbModulusGF _field;
    private readonly int[] _coefficients;

    /// <summary>Creates a polynomial from its coefficients, most significant first.</summary>
    /// <param name="field">The field the coefficients live in.</param>
    /// <param name="coefficients">Coefficients, most significant first.</param>
    public BbModulusPoly(BbModulusGF field, int[] coefficients)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(coefficients);
        if (coefficients.Length == 0)
        {
            throw new ArgumentException("A polynomial needs at least one coefficient.", nameof(coefficients));
        }

        _field = field;

        if (coefficients.Length > 1 && coefficients[0] == 0)
        {
            var firstNonZero = 1;
            while (firstNonZero < coefficients.Length && coefficients[firstNonZero] == 0)
            {
                firstNonZero++;
            }

            _coefficients = firstNonZero == coefficients.Length ? [0] : coefficients[firstNonZero..];
        }
        else
        {
            _coefficients = coefficients;
        }
    }

    /// <summary>The coefficients, most significant first.</summary>
    public int[] Coefficients => _coefficients;

    /// <summary>Degree of the polynomial.</summary>
    public int Degree => _coefficients.Length - 1;

    /// <summary><see langword="true"/> when this is the zero polynomial.</summary>
    public bool IsZero => _coefficients[0] == 0;

    /// <summary>Returns the coefficient of <c>x^degree</c>.</summary>
    /// <param name="degree">Degree of the term.</param>
    public int GetCoefficient(int degree) => _coefficients[_coefficients.Length - 1 - degree];

    /// <summary>Evaluates the polynomial at <paramref name="a"/>.</summary>
    /// <param name="a">The point to evaluate at.</param>
    public int EvaluateAt(int a)
    {
        if (a == 0)
        {
            return GetCoefficient(0);
        }

        if (a == 1)
        {
            var sum = 0;
            foreach (var coefficient in _coefficients)
            {
                sum = _field.Add(sum, coefficient);
            }

            return sum;
        }

        var result = _coefficients[0];
        for (var i = 1; i < _coefficients.Length; i++)
        {
            result = _field.Add(_field.Multiply(a, result), _coefficients[i]);
        }

        return result;
    }

    /// <summary>Adds another polynomial.</summary>
    /// <param name="other">The polynomial to add.</param>
    public BbModulusPoly Add(BbModulusPoly other)
    {
        if (IsZero)
        {
            return other;
        }

        if (other.IsZero)
        {
            return this;
        }

        var smaller = _coefficients;
        var larger = other._coefficients;
        if (smaller.Length > larger.Length)
        {
            (smaller, larger) = (larger, smaller);
        }

        var sum = new int[larger.Length];
        var lengthDiff = larger.Length - smaller.Length;
        Array.Copy(larger, 0, sum, 0, lengthDiff);
        for (var i = lengthDiff; i < larger.Length; i++)
        {
            sum[i] = _field.Add(smaller[i - lengthDiff], larger[i]);
        }

        return new BbModulusPoly(_field, sum);
    }

    /// <summary>Subtracts another polynomial.</summary>
    /// <param name="other">The polynomial to subtract.</param>
    public BbModulusPoly Subtract(BbModulusPoly other) => other.IsZero ? this : Add(other.Negative());

    /// <summary>Returns the additive inverse.</summary>
    public BbModulusPoly Negative()
    {
        var negated = new int[_coefficients.Length];
        for (var i = 0; i < _coefficients.Length; i++)
        {
            negated[i] = _field.Subtract(0, _coefficients[i]);
        }

        return new BbModulusPoly(_field, negated);
    }

    /// <summary>Multiplies by another polynomial.</summary>
    /// <param name="other">The polynomial to multiply by.</param>
    public BbModulusPoly Multiply(BbModulusPoly other)
    {
        if (IsZero || other.IsZero)
        {
            return _field.Zero;
        }

        var a = _coefficients;
        var b = other._coefficients;
        var product = new int[a.Length + b.Length - 1];
        for (var i = 0; i < a.Length; i++)
        {
            var ac = a[i];
            for (var j = 0; j < b.Length; j++)
            {
                product[i + j] = _field.Add(product[i + j], _field.Multiply(ac, b[j]));
            }
        }

        return new BbModulusPoly(_field, product);
    }

    /// <summary>Multiplies every coefficient by a scalar.</summary>
    /// <param name="scalar">The scalar to multiply by.</param>
    public BbModulusPoly Multiply(int scalar)
    {
        if (scalar == 0)
        {
            return _field.Zero;
        }

        if (scalar == 1)
        {
            return this;
        }

        var product = new int[_coefficients.Length];
        for (var i = 0; i < product.Length; i++)
        {
            product[i] = _field.Multiply(_coefficients[i], scalar);
        }

        return new BbModulusPoly(_field, product);
    }

    /// <summary>Multiplies by the monomial <c>coefficient * x^degree</c>.</summary>
    /// <param name="degree">Degree of the monomial.</param>
    /// <param name="coefficient">Coefficient of the monomial.</param>
    public BbModulusPoly MultiplyByMonomial(int degree, int coefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(degree);
        if (coefficient == 0)
        {
            return _field.Zero;
        }

        var product = new int[_coefficients.Length + degree];
        for (var i = 0; i < _coefficients.Length; i++)
        {
            product[i] = _field.Multiply(_coefficients[i], coefficient);
        }

        return new BbModulusPoly(_field, product);
    }
}
