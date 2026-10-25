// PDF function evaluation (Types 0, 2, 3, 4).


namespace Bit.BlazorUI;

/// <summary>
/// Evaluates a PDF function object (used by shadings, transfer functions, etc.).
/// Supports sampled (Type 0), exponential interpolation (Type 2), stitching
/// (Type 3) and a subset of PostScript calculator (Type 4) functions.
/// </summary>
public abstract class BitPdfFunction
{
    /// <summary>Evaluates the function for the given inputs.</summary>
    public abstract double[] Eval(double[] input);

    // A real function nests a few levels at most (a stitching function of stitching
    // functions); these bound a hostile chain, a cycle (a stitching function listing
    // itself in /Functions) and a shared sub-function fanned out at every level -
    // see BitPdfResolveScope.
    private const int MaxNesting = 16;
    private const int MaxNodes = 1024;

    /// <summary>Builds a function from a dictionary/stream object, or <c>null</c> if unsupported.</summary>
    public static BitPdfFunction? Create(object? obj, IBitPdfXRef xref)
        => Create(obj, xref, new BitPdfResolveScope(MaxNesting, MaxNodes));

    /// <summary>
    /// Builds a function within <paramref name="scope"/>, the resolution it is nested in;
    /// <c>null</c> (as for an unsupported function) when the scope refuses it.
    /// </summary>
    internal static BitPdfFunction? Create(object? obj, IBitPdfXRef xref, BitPdfResolveScope scope)
    {
        obj = xref.FetchIfRef(obj);
        if (obj is null || !scope.TryEnter(obj))
        {
            return null;
        }
        try
        {
            return CreateCore(obj, xref, scope);
        }
        finally
        {
            scope.Exit(obj);
        }
    }

    private static BitPdfFunction? CreateCore(object obj, IBitPdfXRef xref, BitPdfResolveScope scope)
    {
        // An array of functions evaluates each and concatenates the outputs.
        if (obj is List<object?> array)
        {
            var parts = new List<BitPdfFunction>();
            foreach (var item in array)
            {
                var fn = Create(item, xref, scope);
                if (fn is not null)
                {
                    parts.Add(fn);
                }
            }
            return parts.Count > 0 ? new ArrayFunction(parts) : null;
        }

        BitPdfDict? dict = obj as BitPdfDict ?? (obj as BitPdfBaseStream)?.Dict;
        if (dict is null)
        {
            return null;
        }

        int type = ToInt(dict.Get("FunctionType"), -1);
        return type switch
        {
            0 when obj is BitPdfBaseStream stream => BitPdfSampledFunction.Build(stream, xref),
            2 => BitPdfExponentialFunction.Build(dict, xref),
            3 => BitPdfStitchingFunction.Build(dict, xref, scope),
            4 when obj is BitPdfBaseStream calc => BitPdfPostScriptFunction.Build(calc),
            _ => null,
        };
    }

    protected static double[] ReadNumbers(object? value, IBitPdfXRef? xref = null)
    {
        if (value is not List<object?> arr)
        {
            return [];
        }
        var result = new double[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            // Elements (/Domain, /C0, /C1, /Range, /Encode, …) may be indirect refs (1.26).
            result[i] = BitPdfPrimitives.ResolveNumber(xref, arr[i]);
        }
        return result;
    }

    protected static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    protected static int ToInt(object? value, int fallback) => value is double d ? (int)d : fallback;

    private sealed class ArrayFunction : BitPdfFunction
    {
        private readonly List<BitPdfFunction> _functions;
        public ArrayFunction(List<BitPdfFunction> functions) => _functions = functions;

        public override double[] Eval(double[] input)
        {
            var output = new List<double>();
            foreach (var fn in _functions)
            {
                output.AddRange(fn.Eval(input));
            }
            return output.ToArray();
        }
    }
}
