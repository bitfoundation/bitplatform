namespace Bit.BbScanner;

/// <summary>
/// A polynomial over a <see cref="BbGenericGF"/>, stored most significant coefficient first.
/// </summary>
public sealed class BbGfPoly
{
    private readonly BbGenericGF _field;
    private readonly int[] _coefficients;

    /// <summary>Creates a polynomial from its coefficients, most significant first.</summary>
    /// <param name="field">The field the coefficients live in.</param>
    /// <param name="coefficients">Coefficients, most significant first.</param>
    public BbGfPoly(BbGenericGF field, int[] coefficients)
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

            _coefficients = firstNonZero == coefficients.Length
                ? [0]
                : coefficients[firstNonZero..];
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
                sum = BbGenericGF.AddOrSubtract(sum, coefficient);
            }

            return sum;
        }

        var result = _coefficients[0];
        for (var i = 1; i < _coefficients.Length; i++)
        {
            result = BbGenericGF.AddOrSubtract(_field.Multiply(a, result), _coefficients[i]);
        }

        return result;
    }

    /// <summary>Adds another polynomial, which in characteristic two is also subtraction.</summary>
    /// <param name="other">The polynomial to add.</param>
    public BbGfPoly AddOrSubtract(BbGfPoly other)
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
            sum[i] = BbGenericGF.AddOrSubtract(smaller[i - lengthDiff], larger[i]);
        }

        return new BbGfPoly(_field, sum);
    }

    /// <summary>Multiplies by another polynomial.</summary>
    /// <param name="other">The polynomial to multiply by.</param>
    public BbGfPoly Multiply(BbGfPoly other)
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
                product[i + j] = BbGenericGF.AddOrSubtract(product[i + j], _field.Multiply(ac, b[j]));
            }
        }

        return new BbGfPoly(_field, product);
    }

    /// <summary>Multiplies every coefficient by a scalar.</summary>
    /// <param name="scalar">The scalar to multiply by.</param>
    public BbGfPoly Multiply(int scalar)
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

        return new BbGfPoly(_field, product);
    }

    /// <summary>Multiplies by the monomial <c>coefficient * x^degree</c>.</summary>
    /// <param name="degree">Degree of the monomial.</param>
    /// <param name="coefficient">Coefficient of the monomial.</param>
    public BbGfPoly MultiplyByMonomial(int degree, int coefficient)
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

        return new BbGfPoly(_field, product);
    }

    /// <summary>Divides by another polynomial, returning the quotient and the remainder.</summary>
    /// <param name="other">The divisor.</param>
    public (BbGfPoly Quotient, BbGfPoly Remainder) Divide(BbGfPoly other)
    {
        if (other.IsZero)
        {
            throw new DivideByZeroException("Cannot divide a polynomial by zero.");
        }

        var quotient = _field.Zero;
        var remainder = this;

        var denominatorLeadingTerm = other.GetCoefficient(other.Degree);
        var inverseDenominatorLeadingTerm = _field.Inverse(denominatorLeadingTerm);

        while (remainder.Degree >= other.Degree && !remainder.IsZero)
        {
            var degreeDifference = remainder.Degree - other.Degree;
            var scale = _field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            var term = other.MultiplyByMonomial(degreeDifference, scale);
            quotient = quotient.AddOrSubtract(_field.BuildMonomial(degreeDifference, scale));
            remainder = remainder.AddOrSubtract(term);
        }

        return (quotient, remainder);
    }
}
