namespace Bit.BbScanner;

/// <summary>
/// A Galois field of the form GF(2^m), the arithmetic every byte oriented Reed-Solomon code
/// in this library is built on.
/// </summary>
/// <remarks>
/// Multiplication is done through pre-computed exponent and logarithm tables, so a field
/// multiply is two table reads and an add. The tables are built once per field and the fields
/// themselves are cached statics, which keeps error correction free of allocations.
/// </remarks>
public sealed class BbGenericGF
{
    /// <summary>The field used by QR Code, with primitive polynomial x^8 + x^4 + x^3 + x^2 + 1.</summary>
    public static BbGenericGF QrCodeField256 { get; } = new(0x011D, 256, 0);

    /// <summary>The field used by Data Matrix ECC 200, with primitive polynomial x^8 + x^5 + x^3 + x^2 + 1.</summary>
    public static BbGenericGF DataMatrixField256 { get; } = new(0x012D, 256, 1);

    private readonly int[] _expTable;
    private readonly int[] _logTable;

    /// <summary>Creates a field.</summary>
    /// <param name="primitive">The primitive polynomial, with the high bit set.</param>
    /// <param name="size">Number of elements in the field.</param>
    /// <param name="generatorBase">Exponent of the first root of the generator polynomial: 0 for QR, 1 for Data Matrix.</param>
    public BbGenericGF(int primitive, int size, int generatorBase)
    {
        Primitive = primitive;
        Size = size;
        GeneratorBase = generatorBase;

        _expTable = new int[size];
        _logTable = new int[size];

        var x = 1;
        for (var i = 0; i < size; i++)
        {
            _expTable[i] = x;
            x <<= 1;
            if (x >= size)
            {
                x ^= primitive;
                x &= size - 1;
            }
        }

        for (var i = 0; i < size - 1; i++)
        {
            _logTable[_expTable[i]] = i;
        }

        Zero = new BbGfPoly(this, [0]);
        One = new BbGfPoly(this, [1]);
    }

    /// <summary>The primitive polynomial of the field.</summary>
    public int Primitive { get; }

    /// <summary>Number of elements in the field.</summary>
    public int Size { get; }

    /// <summary>Exponent of the first root of the generator polynomial.</summary>
    public int GeneratorBase { get; }

    /// <summary>The zero polynomial in this field.</summary>
    public BbGfPoly Zero { get; }

    /// <summary>The constant polynomial 1 in this field.</summary>
    public BbGfPoly One { get; }

    /// <summary>Returns the monomial <c>coefficient * x^degree</c>.</summary>
    /// <param name="degree">Degree of the term.</param>
    /// <param name="coefficient">Coefficient of the term.</param>
    public BbGfPoly BuildMonomial(int degree, int coefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(degree);
        if (coefficient == 0)
        {
            return Zero;
        }

        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new BbGfPoly(this, coefficients);
    }

    /// <summary>Addition and subtraction coincide in a field of characteristic two.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    public static int AddOrSubtract(int a, int b) => a ^ b;

    /// <summary>Returns 2 raised to the power <paramref name="a"/> in the field.</summary>
    /// <param name="a">Exponent.</param>
    public int Exp(int a) => _expTable[a];

    /// <summary>Returns the base 2 logarithm of <paramref name="a"/> in the field.</summary>
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

        return _expTable[Size - _logTable[a] - 1];
    }

    /// <summary>Multiplies two field elements.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    public int Multiply(int a, int b) =>
        a == 0 || b == 0 ? 0 : _expTable[(_logTable[a] + _logTable[b]) % (Size - 1)];
}
