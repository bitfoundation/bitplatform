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

    /// <summary>Builds a function from a dictionary/stream object, or <c>null</c> if unsupported.</summary>
    public static BitPdfFunction? Create(object? obj, IBitPdfXRef xref)
    {
        obj = xref.FetchIfRef(obj);

        // An array of functions evaluates each and concatenates the outputs.
        if (obj is List<object?> array)
        {
            var parts = new List<BitPdfFunction>();
            foreach (var item in array)
            {
                var fn = Create(item, xref);
                if (fn is not null)
                {
                    parts.Add(fn);
                }
            }
            return parts.Count > 0 ? new ArrayFunction(parts) : null;
        }

        BitPdfDict? dict = obj as BitPdfDict ?? (obj as BitPdfStream)?.Dict;
        if (dict is null)
        {
            return null;
        }

        int type = ToInt(dict.Get("FunctionType"), -1);
        return type switch
        {
            0 when obj is BitPdfStream stream => BitPdfSampledFunction.Build(stream, xref),
            2 => BitPdfExponentialFunction.Build(dict, xref),
            3 => BitPdfStitchingFunction.Build(dict, xref),
            4 when obj is BitPdfStream calc => BitPdfPostScriptFunction.Build(calc),
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
