namespace Bit.BbScanner;

/// <summary>
/// A prime Galois field, GF(929), which is the arithmetic PDF417 error correction is defined
/// over.
/// </summary>
/// <remarks>
/// Unlike the byte oriented symbologies, PDF417 codewords take 929 values, so its field is a
/// prime field rather than an extension field. Addition is therefore ordinary modular addition
/// and no longer coincides with subtraction, which is why this cannot share an implementation
/// with <see cref="BbGenericGF"/>.
/// </remarks>
public sealed class BbModulusGF
{
    /// <summary>The field used by PDF417: 929 elements with 3 as the generator.</summary>
    public static BbModulusGF Pdf417 { get; } = new(929, 3);

    private readonly int[] _expTable;
    private readonly int[] _logTable;

    /// <summary>Creates a prime field.</summary>
    /// <param name="modulus">The prime modulus.</param>
    /// <param name="generator">A primitive root of the field.</param>
    public BbModulusGF(int modulus, int generator)
    {
        Modulus = modulus;
        _expTable = new int[modulus];
        _logTable = new int[modulus];

        var x = 1;
        for (var i = 0; i < modulus; i++)
        {
            _expTable[i] = x;
            x = (x * generator) % modulus;
        }

        for (var i = 0; i < modulus - 1; i++)
        {
            _logTable[_expTable[i]] = i;
        }

        Zero = new BbModulusPoly(this, [0]);
        One = new BbModulusPoly(this, [1]);
    }

    /// <summary>Number of elements in the field.</summary>
    public int Modulus { get; }

    /// <summary>The zero polynomial.</summary>
    public BbModulusPoly Zero { get; }

    /// <summary>The constant polynomial 1.</summary>
    public BbModulusPoly One { get; }

    /// <summary>Returns the monomial <c>coefficient * x^degree</c>.</summary>
    /// <param name="degree">Degree of the term.</param>
    /// <param name="coefficient">Coefficient of the term.</param>
    public BbModulusPoly BuildMonomial(int degree, int coefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(degree);
        if (coefficient == 0)
        {
            return Zero;
        }

        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new BbModulusPoly(this, coefficients);
    }

    /// <summary>Adds two field elements.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    public int Add(int a, int b) => (a + b) % Modulus;

    /// <summary>Subtracts two field elements.</summary>
    /// <param name="a">Minuend.</param>
    /// <param name="b">Subtrahend.</param>
    public int Subtract(int a, int b) => (Modulus + a - b) % Modulus;

    /// <summary>Returns the generator raised to the power <paramref name="a"/>.</summary>
    /// <param name="a">Exponent.</param>
    public int Exp(int a) => _expTable[a];

    /// <summary>Returns the discrete logarithm of <paramref name="a"/>.</summary>
    /// <param name="a">A non-zero field element.</param>
    public int Log(int a)
    {
        if (a == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), "The logarithm of zero is undefined.");
        }

        return _logTable[a];
    }

    /// <summary>Returns the multiplicative inverse of <paramref name="a"/>.</summary>
    /// <param name="a">A non-zero field element.</param>
    public int Inverse(int a)
    {
        if (a == 0)
        {
            throw new ArithmeticException("Zero has no multiplicative inverse.");
        }

        return _expTable[Modulus - _logTable[a] - 1];
    }

    /// <summary>Multiplies two field elements.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    public int Multiply(int a, int b) => a == 0 || b == 0 ? 0 : (a * b) % Modulus;
}
