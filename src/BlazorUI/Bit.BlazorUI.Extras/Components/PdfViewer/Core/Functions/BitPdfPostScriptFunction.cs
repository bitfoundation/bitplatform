// Type 4 (PostScript calculator) function parsing and evaluation.

using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Evaluates a Type 4 PostScript calculator function: a small stack language
/// limited to arithmetic, comparison and conditional operators.
/// </summary>
internal sealed class BitPdfPostScriptFunction : BitPdfFunction
{
    // pdf.js caps the operand stack at 100; a well-formed calculator function
    // never needs more, and the cap bounds copy/roll/dup against DoS.
    private const int MaxStack = 100;

    private readonly List<object> _program; // doubles, string operators, and nested List<object> blocks
    private readonly double[] _domain;
    private readonly double[] _range;

    private BitPdfPostScriptFunction(List<object> program, double[] domain, double[] range)
    {
        _program = program;
        _domain = domain;
        _range = range;
    }

    public static BitPdfPostScriptFunction? Build(BitPdfStream stream)
    {
        BitPdfDict dict = stream.Dict!;
        double[] domain = ReadNumbers(dict.Get("Domain"));
        double[] range = ReadNumbers(dict.Get("Range"));
        if (range.Length < 2)
        {
            return null;
        }

        string source = System.Text.Encoding.Latin1.GetString(BitPdfStreamDecoder.Decode(stream));
        var tokens = Tokenize(source);
        int pos = 0;
        // Skip to the outermost '{'.
        while (pos < tokens.Count && tokens[pos] != "{")
        {
            pos++;
        }
        var program = ParseBlock(tokens, ref pos);
        return new BitPdfPostScriptFunction(program, domain.Length >= 2 ? domain : [0, 1], range);
    }

    public override double[] Eval(double[] input)
    {
        var stack = new Stack<double>();
        foreach (double v in input)
        {
            stack.Push(v);
        }

        Run(_program, stack);

        int outCount = _range.Length / 2;
        var output = new double[outCount];
        for (int i = outCount - 1; i >= 0; i--)
        {
            double v = stack.Count > 0 ? stack.Pop() : 0;
            output[i] = Clamp(v, _range[i * 2], _range[i * 2 + 1]);
        }
        return output;
    }

    private static void Run(List<object> program, Stack<double> stack)
    {
        for (int i = 0; i < program.Count; i++)
        {
            object token = program[i];
            if (token is double d)
            {
                stack.Push(d);
                continue;
            }
            if (token is List<object>)
            {
                continue; // blocks are consumed by if/ifelse below
            }

            string op = (string)token;
            switch (op)
            {
                case "add": Bin(stack, static (a, b) => a + b); break;
                case "sub": Bin(stack, static (a, b) => a - b); break;
                case "mul": Bin(stack, static (a, b) => a * b); break;
                case "div": Bin(stack, static (a, b) => b != 0 ? a / b : 0); break;
                case "idiv": Bin(stack, static (a, b) => b != 0 ? Math.Truncate(a / b) : 0); break;
                case "mod": Bin(stack, static (a, b) => b != 0 ? a % b : 0); break;
                case "neg": Un(stack, static a => -a); break;
                case "abs": Un(stack, Math.Abs); break;
                case "sqrt": Un(stack, static a => Math.Sqrt(Math.Max(0, a))); break;
                case "sin": Un(stack, static a => Math.Sin(a * Math.PI / 180)); break;
                case "cos": Un(stack, static a => Math.Cos(a * Math.PI / 180)); break;
                case "atan": Bin(stack, static (a, b) => { double r = Math.Atan2(a, b) * 180 / Math.PI; return r < 0 ? r + 360 : r; }); break;
                case "exp": Bin(stack, Math.Pow); break;
                case "ln": Un(stack, static a => a > 0 ? Math.Log(a) : 0); break;
                case "log": Un(stack, static a => a > 0 ? Math.Log10(a) : 0); break;
                case "cvi": case "truncate": Un(stack, Math.Truncate); break;
                case "cvr": break;
                case "floor": Un(stack, Math.Floor); break;
                case "ceiling": Un(stack, Math.Ceiling); break;
                case "round": Un(stack, static a => Math.Round(a, MidpointRounding.AwayFromZero)); break;
                case "dup": { double a = Peek(stack); stack.Push(a); break; }
                case "pop": if (stack.Count > 0) stack.Pop(); break;
                case "exch": { double b = Pop(stack), a = Pop(stack); stack.Push(b); stack.Push(a); break; }
                case "copy": DoCopy(stack); break;
                case "index": DoIndex(stack); break;
                case "roll": DoRoll(stack); break;
                case "eq": Cmp(stack, static (a, b) => a == b); break;
                case "ne": Cmp(stack, static (a, b) => a != b); break;
                case "gt": Cmp(stack, static (a, b) => a > b); break;
                case "ge": Cmp(stack, static (a, b) => a >= b); break;
                case "lt": Cmp(stack, static (a, b) => a < b); break;
                case "le": Cmp(stack, static (a, b) => a <= b); break;
                case "and": Bin(stack, static (a, b) => (double)((long)a & (long)b)); break;
                case "or": Bin(stack, static (a, b) => (double)((long)a | (long)b)); break;
                case "xor": Bin(stack, static (a, b) => (double)((long)a ^ (long)b)); break;
                // A negative shift is a LOGICAL right shift (zeros fill the vacated
                // bits regardless of sign, PLRM bitshift) — shift unsigned, not v >> -s.
                case "bitshift": Bin(stack, static (a, b) => { int s = (int)b; long v = (long)a; return s >= 0 ? (double)(v << s) : (double)(long)((ulong)v >> -s); }); break;
                case "not": Un(stack, static a => a != 0 ? 0 : 1); break;
                case "true": stack.Push(1); break;
                case "false": stack.Push(0); break;
                case "if":
                    if (i >= 1 && program[i - 1] is List<object> ifBlock)
                    {
                        double cond = Pop(stack);
                        if (cond != 0)
                        {
                            Run(ifBlock, stack);
                        }
                    }
                    break;
                case "ifelse":
                    if (i >= 2 && program[i - 2] is List<object> b1 && program[i - 1] is List<object> b2)
                    {
                        double cond = Pop(stack);
                        Run(cond != 0 ? b1 : b2, stack);
                    }
                    break;
            }
        }
    }

    // The proc blocks for if/ifelse are pushed implicitly; when we encounter a
    // block token we must remember it. Adjust Run to push block markers.

    private static void Bin(Stack<double> s, Func<double, double, double> f)
    {
        double b = Pop(s), a = Pop(s);
        s.Push(f(a, b));
    }

    private static void Un(Stack<double> s, Func<double, double> f) => s.Push(f(Pop(s)));

    private static void Cmp(Stack<double> s, Func<double, double, bool> f)
    {
        double b = Pop(s), a = Pop(s);
        s.Push(f(a, b) ? 1 : 0);
    }

    private static void DoCopy(Stack<double> s)
    {
        int n = (int)Pop(s);
        // Reject unbounded / out-of-range counts: copy only duplicates elements
        // that exist, and must not push the stack past the operand-stack cap.
        if (n <= 0 || n > s.Count || s.Count + n > MaxStack) return;
        var arr = s.ToArray(); // top first
        for (int i = n - 1; i >= 0; i--)
        {
            if (i < arr.Length)
            {
                s.Push(arr[i]);
            }
        }
    }

    private static void DoIndex(Stack<double> s)
    {
        int n = (int)Pop(s);
        var arr = s.ToArray();
        s.Push(n >= 0 && n < arr.Length ? arr[n] : 0);
    }

    private static void DoRoll(Stack<double> s)
    {
        int j = (int)Pop(s);
        int n = (int)Pop(s);
        // Bound n by the actual stack depth so a hostile count can't allocate a
        // 2-billion-element array or spin an enormous loop.
        if (n <= 0 || n > s.Count) return;
        var items = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            items[i] = Pop(s);
        }
        j = ((j % n) + n) % n;
        for (int i = 0; i < n; i++)
        {
            s.Push(items[(i - j + n) % n]);
        }
    }

    private static double Pop(Stack<double> s) => s.Count > 0 ? s.Pop() : 0;
    private static double Peek(Stack<double> s) => s.Count > 0 ? s.Peek() : 0;

    private static List<string> Tokenize(string src)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c is '{' or '}') { tokens.Add(c.ToString()); i++; continue; }
            if (c == '%') { while (i < src.Length && src[i] is not ('\n' or '\r')) i++; continue; }
            int start = i;
            while (i < src.Length && !char.IsWhiteSpace(src[i]) && src[i] is not ('{' or '}'))
            {
                i++;
            }
            tokens.Add(src[start..i]);
        }
        return tokens;
    }

    private static List<object> ParseBlock(List<string> tokens, ref int pos)
    {
        var block = new List<object>();
        if (pos < tokens.Count && tokens[pos] == "{")
        {
            pos++; // consume '{'
        }
        while (pos < tokens.Count && tokens[pos] != "}")
        {
            string t = tokens[pos];
            if (t == "{")
            {
                block.Add(ParseBlock(tokens, ref pos));
            }
            else
            {
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                {
                    block.Add(num);
                }
                else
                {
                    block.Add(t);
                }
                pos++;
            }
        }
        if (pos < tokens.Count && tokens[pos] == "}")
        {
            pos++; // consume '}'
        }
        return block;
    }
}
