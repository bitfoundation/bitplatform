using System.Globalization;
using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Reads SVG path data (the <c>d</c> attribute of an SVG <c>path</c>) into a <see cref="BinkPathGeometry"/>. Writing
/// goes the other way through <see cref="BinkPathGeometry.ToSvgPathData"/>.
/// </summary>
/// <remarks>
/// Every command of the SVG 1.1 path grammar is understood, absolute and relative: M, L, H, V, C, S, Q, T, A and Z.
/// Arcs become cubic Bézier curves, the only curves the geometry stores, with at most a quarter turn per curve so
/// they are indistinguishable from true arcs. The input is untrusted: its length and the number of segments it may
/// produce are bounded, and anything malformed is reported with where it went wrong rather than read halfway.
/// </remarks>
public static class BinkSvgPathData
{
    /// <summary>The longest path data accepted, in characters.</summary>
    public const int MaxLength = 4_000_000;

    /// <summary>Parses path data.</summary>
    /// <param name="data">The path data, for example <c>"M 10 10 L 90 10 A 40 40 0 0 1 10 10 Z"</c>.</param>
    /// <param name="maxSegments">The most segments (lines, curves, moves) the path may produce.</param>
    /// <exception cref="FormatException">The data is malformed, too long, or produces too many segments.</exception>
    public static BinkPathGeometry Parse(string data, int maxSegments = 100_000)
    {
        ArgumentNullException.ThrowIfNull(data);
        return TryParse(data, out BinkPathGeometry? path, out string? error, maxSegments) ? path! : throw new FormatException(error);
    }

    /// <summary>Parses path data, reporting a problem instead of throwing.</summary>
    /// <param name="data">The path data.</param>
    /// <param name="path">The geometry, or null when the data could not be read.</param>
    /// <param name="error">What was wrong, or null.</param>
    /// <param name="maxSegments">The most segments the path may produce.</param>
    public static bool TryParse(string? data, out BinkPathGeometry? path, out string? error, int maxSegments = 100_000)
    {
        path = null;
        if (data is null)
        {
            error = "There is no path data.";
            return false;
        }
        if (data.Length > MaxLength)
        {
            error = $"The path data is {data.Length} characters long; the limit is {MaxLength}.";
            return false;
        }
        var reader = new Reader(data, Math.Max(maxSegments, 1));
        try
        {
            path = reader.Read();
            error = null;
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<char> _text;
        private readonly int _maxSegments;
        private readonly BinkPathBuilder _builder;
        private int _position;
        private int _segments;
        private Vector2 _current;
        private Vector2 _subpathStart;
        private Vector2 _lastControl;
        private char _lastCommand;

        public Reader(string text, int maxSegments)
        {
            _text = text;
            _maxSegments = maxSegments;
            _builder = new BinkPathBuilder(Math.Clamp(text.Length / 4, 16, 1 << 16));
        }

        public BinkPathGeometry Read()
        {
            SkipSeparators();
            char command = '\0';
            while (_position < _text.Length)
            {
                char c = _text[_position];
                if (IsCommand(c))
                {
                    command = c;
                    _position++;
                }
                else if (command == '\0') throw Error("Path data must start with a command.");
                else if (command is 'Z' or 'z') throw Error("A close command takes no numbers.");
                // A command letter may be followed by several sets of numbers; a moveto's extra sets are linetos.
                else if (command == 'M') command = 'L';
                else if (command == 'm') command = 'l';

                if (_lastCommand == '\0' && command is not ('M' or 'm')) throw Error("Path data must start with a moveto.");
                Execute(command);
                _lastCommand = command;
                SkipSeparators();
            }
            return _builder.ToGeometry();
        }

        private void Execute(char command)
        {
            bool relative = char.IsLower(command);
            Vector2 origin = relative ? _current : Vector2.Zero;
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    _current = origin + ReadPoint();
                    _subpathStart = _current;
                    Count();
                    _builder.MoveTo(_current);
                    _lastControl = _current;
                    break;
                case 'L':
                    LineTo(origin + ReadPoint());
                    break;
                case 'H':
                    LineTo(new Vector2((relative ? _current.X : 0f) + ReadNumber(), _current.Y));
                    break;
                case 'V':
                    LineTo(new Vector2(_current.X, (relative ? _current.Y : 0f) + ReadNumber()));
                    break;
                case 'C':
                {
                    Vector2 c1 = origin + ReadPoint();
                    Vector2 c2 = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    CubicTo(c1, c2, end);
                    break;
                }
                case 'S':
                {
                    // The first control point mirrors the previous curve's second, when the previous command was a cubic.
                    Vector2 c1 = _lastCommand is 'C' or 'c' or 'S' or 's' ? 2 * _current - _lastControl : _current;
                    Vector2 c2 = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    CubicTo(c1, c2, end);
                    break;
                }
                case 'Q':
                {
                    Vector2 control = origin + ReadPoint();
                    Vector2 end = origin + ReadPoint();
                    QuadTo(control, end);
                    break;
                }
                case 'T':
                {
                    Vector2 control = _lastCommand is 'Q' or 'q' or 'T' or 't' ? 2 * _current - _lastControl : _current;
                    QuadTo(control, origin + ReadPoint());
                    break;
                }
                case 'A':
                {
                    float rx = ReadNumber();
                    float ry = ReadNumber();
                    float rotation = ReadNumber();
                    bool large = ReadFlag();
                    bool sweep = ReadFlag();
                    Vector2 end = origin + ReadPoint();
                    ArcTo(rx, ry, rotation, large, sweep, end);
                    break;
                }
                case 'Z':
                    Count();
                    _builder.Close();
                    _current = _subpathStart;
                    _lastControl = _current;
                    break;
            }
        }

        private void LineTo(Vector2 point)
        {
            Count();
            _builder.LineTo(point);
            _current = point;
            _lastControl = point;
        }

        private void CubicTo(Vector2 c1, Vector2 c2, Vector2 end)
        {
            Count();
            _builder.CubicTo(c1, c2, end);
            _current = end;
            _lastControl = c2;
        }

        private void QuadTo(Vector2 control, Vector2 end)
        {
            Count();
            _builder.QuadTo(control, end);
            _current = end;
            _lastControl = control;
        }

        /// <summary>
        /// An elliptical arc, converted to centre form as the SVG implementation notes describe (F.6.5) and drawn as
        /// one cubic per quarter turn at most.
        /// </summary>
        private void ArcTo(float radiusX, float radiusY, float rotationDegrees, bool large, bool sweep, Vector2 end)
        {
            Vector2 start = _current;
            // A zero radius, or an arc that ends where it starts, is a straight line or nothing (F.6.2).
            if (start == end) return;
            if (MathF.Abs(radiusX) < BinkGeometryMath.Epsilon || MathF.Abs(radiusY) < BinkGeometryMath.Epsilon)
            {
                LineTo(end);
                return;
            }

            // Worked in double: the radii can be anything a float holds, and their squares multiplied overflow a float,
            // while a large radius puts the centre far enough away that float rounding would visibly move the arc.
            double rx = Math.Abs((double)radiusX);
            double ry = Math.Abs((double)radiusY);
            double phi = rotationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(phi);
            double sin = Math.Sin(phi);
            double hx = ((double)start.X - end.X) / 2.0;
            double hy = ((double)start.Y - end.Y) / 2.0;
            double px = cos * hx + sin * hy;
            double py = -sin * hx + cos * hy;

            // Radii too small to reach the end point are scaled up just enough (F.6.6).
            double lambda = px * px / (rx * rx) + py * py / (ry * ry);
            if (lambda > 1.0)
            {
                double scale = Math.Sqrt(lambda);
                rx *= scale;
                ry *= scale;
            }
            // The short way round an ellipse ten million times wider than the gap is that gap, to well within a float's
            // precision. Drawn from its centre, it would be lost to rounding instead.
            if (!large && Math.Min(rx, ry) > 1e7 * Math.Sqrt(px * px + py * py))
            {
                LineTo(end);
                return;
            }

            double numerator = rx * rx * ry * ry - rx * rx * py * py - ry * ry * px * px;
            double denominator = rx * rx * py * py + ry * ry * px * px;
            double factor = denominator <= 0.0 ? 0.0 : Math.Sqrt(Math.Max(numerator, 0.0) / denominator);
            if (large == sweep) factor = -factor;
            double cxp = factor * rx * py / ry;
            double cyp = -factor * ry * px / rx;
            double cx = cos * cxp - sin * cyp + ((double)start.X + end.X) / 2.0;
            double cy = sin * cxp + cos * cyp + ((double)start.Y + end.Y) / 2.0;

            double ux = (px - cxp) / rx, uy = (py - cyp) / ry;
            double vx = (-px - cxp) / rx, vy = (-py - cyp) / ry;
            double theta = Angle(1.0, 0.0, ux, uy);
            double delta = Angle(ux, uy, vx, vy);
            if (!sweep && delta > 0) delta -= Math.Tau;
            else if (sweep && delta < 0) delta += Math.Tau;

            int pieces = Math.Max((int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2.0) - 1e-4), 1);
            double step = delta / pieces;
            // The control point distance that makes a cubic follow a circular arc of the given angle.
            double k = 4.0 / 3.0 * Math.Tan(step / 4.0);
            for (int i = 0; i < pieces; i++)
            {
                double a1 = theta + i * step;
                double a2 = a1 + step;
                (double x1, double y1) = PointOn(a1);
                (double x2, double y2) = i == pieces - 1 ? ((double)end.X, (double)end.Y) : PointOn(a2);
                (double dx1, double dy1) = DerivativeOn(a1);
                (double dx2, double dy2) = DerivativeOn(a2);
                var control1 = new Vector2((float)(x1 + dx1 * k), (float)(y1 + dy1 * k));
                var control2 = new Vector2((float)(x2 - dx2 * k), (float)(y2 - dy2 * k));
                var to = new Vector2((float)x2, (float)y2);
                // An arc around an ellipse near a float's range reaches beyond it.
                if (!IsFinite(control1) || !IsFinite(control2) || !IsFinite(to)) throw Error("An arc is too large to draw.");
                CubicTo(control1, control2, to);
            }

            (double X, double Y) PointOn(double angle)
            {
                double x = rx * Math.Cos(angle);
                double y = ry * Math.Sin(angle);
                return (cx + cos * x - sin * y, cy + sin * x + cos * y);
            }

            (double X, double Y) DerivativeOn(double angle)
            {
                double x = -rx * Math.Sin(angle);
                double y = ry * Math.Cos(angle);
                return (cos * x - sin * y, sin * x + cos * y);
            }
        }

        private static bool IsFinite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);

        private static double Angle(double ux, double uy, double vx, double vy)
        {
            double angle = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            return double.IsFinite(angle) ? angle : 0.0;
        }

        private void Count()
        {
            if (++_segments > _maxSegments) throw Error($"The path has more than {_maxSegments} segments.");
        }

        private Vector2 ReadPoint() => new(ReadNumber(), ReadNumber());

        private bool ReadFlag()
        {
            SkipSeparators();
            // Flags may be written without a separator after them ("a10 10 0 01 50 50").
            if (_position < _text.Length && _text[_position] is '0' or '1') return _text[_position++] == '1';
            throw Error("An arc flag must be 0 or 1.");
        }

        private float ReadNumber()
        {
            SkipSeparators();
            int start = _position;
            if (_position < _text.Length && _text[_position] is '+' or '-') _position++;
            bool digits = false;
            while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) { _position++; digits = true; }
            // A second point starts a new number: "0.5.5" is 0.5 and .5.
            if (_position < _text.Length && _text[_position] == '.')
            {
                _position++;
                while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) { _position++; digits = true; }
            }
            if (!digits)
            {
                _position = start;
                throw Error("A number was expected.");
            }
            if (_position < _text.Length && _text[_position] is 'e' or 'E')
            {
                _position++;
                if (_position < _text.Length && _text[_position] is '+' or '-') _position++;
                int exponentDigits = _position;
                while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++;
                // An 'e' with no digits after it is not an exponent (it cannot be a command either), so it is malformed.
                if (_position == exponentDigits) throw Error("An exponent needs digits.");
            }
            if (!float.TryParse(_text[start.._position], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value))
            {
                throw Error("A number is out of range.");
            }
            return value;
        }

        private void SkipSeparators()
        {
            while (_position < _text.Length && (_text[_position] is ',' || char.IsWhiteSpace(_text[_position]))) _position++;
        }

        private static bool IsCommand(char c) => c is 'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v' or 'C' or 'c' or 'S' or 's' or 'Q' or 'q' or 'T' or 't' or 'A' or 'a' or 'Z' or 'z';

        private readonly FormatException Error(string message) => new(FormattableString.Invariant($"{message} (at character {_position})"));
    }
}
