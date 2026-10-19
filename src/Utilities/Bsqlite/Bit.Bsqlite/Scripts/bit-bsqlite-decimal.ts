// A .NET System.Decimal, in JavaScript.
//
// EF Core stores decimals as TEXT and asks the worker to add, divide, compare, and order them. IEEE
// floats cannot do that: 0.1 is already wrong, and 1/3 must come back as twenty-eight threes, not as
// 0.3333333333333333. The representation is the same one .NET uses - a 96-bit coefficient, a scale
// of 0–28, and a sign - so arithmetic here answers what System.Decimal would answer.
//
// Two text forms come out of that, and the distinction matters. `toString` is .NET's ToString and
// keeps the value's own scale. `toSqlText` is the canonical form the SQLite stack stores, which is
// what EF compares against with `=`; that is the one every `ef_*` result is written in.

const MAX_COEFF = (1n << 96n) - 1n;
const MAX_SCALE = 28;

export class DecimalOverflowError extends Error {
    constructor() {
        super('Decimal overflow.');
        this.name = 'DecimalOverflowError';
    }
}

export class DecimalFormatError extends Error {
    constructor(text: unknown) {
        super(`'${text}' is not a decimal.`);
        this.name = 'DecimalFormatError';
    }
}

export type DecimalSign = 1 | -1;

export class Decimal {
    readonly sign: DecimalSign;

    /** The unsigned coefficient. */
    readonly coeff: bigint;

    /** Digits after the decimal point, 0–28. */
    readonly scale: number;

    constructor(sign: DecimalSign, coeff: bigint, scale: number) {
        this.sign = coeff === 0n ? 1 : sign;
        this.coeff = coeff;
        this.scale = scale;
    }

    static zero() {
        return new Decimal(1, 0n, 0);
    }

    /**
     * Parses invariant-culture decimal text. Leading and trailing whitespace and a leading sign are
     * accepted; thousands separators and exponents are not - EF never writes those.
     */
    static parse(text: string): Decimal {
        if (typeof text !== 'string') {
            throw new DecimalFormatError(text);
        }

        const trimmed = text.trim();
        if (!trimmed) {
            throw new DecimalFormatError(text);
        }

        let sign: DecimalSign = 1;
        let body = trimmed;
        if (body[0] === '+' || body[0] === '-') {
            sign = body[0] === '-' ? -1 : 1;
            body = body.slice(1);
        }

        if (!body || /[^0-9.]/.test(body)) {
            throw new DecimalFormatError(text);
        }

        const dot = body.indexOf('.');
        if (dot !== body.lastIndexOf('.')) {
            throw new DecimalFormatError(text);
        }

        const digits = (dot === -1 ? body : body.slice(0, dot) + body.slice(dot + 1)).replace(/^0+(?=\d)/, '');
        const scale = dot === -1 ? 0 : body.length - dot - 1;

        if (scale > MAX_SCALE) {
            throw new DecimalFormatError(text);
        }

        if (!digits) {
            return Decimal.zero();
        }

        const coeff = BigInt(digits);
        if (coeff > MAX_COEFF) {
            throw new DecimalOverflowError();
        }

        return new Decimal(sign, coeff, scale);
    }

    /** Invariant ToString, including trailing zeros that the scale requires (`4.0`, not `4`). */
    toString(): string {
        const digits = this.coeff.toString();
        const sign = this.sign < 0 ? '-' : '';

        if (this.scale === 0) {
            return sign + digits;
        }

        const padded = digits.length <= this.scale
            ? digits.padStart(this.scale + 1, '0')
            : digits;
        const split = padded.length - this.scale;
        return `${sign}${padded.slice(0, split)}.${padded.slice(split)}`;
    }

    /**
     * The canonical TEXT form the rest of the SQLite/EF stack writes, which is
     * Microsoft.Data.Sqlite's `0.0###########################` format rather than a plain ToString.
     *
     * The two differ: ToString keeps the value's own scale (`10` stays `10`, `1.50` stays `1.50`),
     * while the canonical form always shows one decimal and trims the rest (`10.0`, `1.5`). EF Core
     * compiles decimal equality to a plain TEXT comparison against a literal it renders in the
     * canonical form, so a value written any other way would silently never match. Every `ef_*`
     * result therefore leaves through here, not through {@link toString}.
     */
    toSqlText(): string {
        const plain = this.toString();
        const dot = plain.indexOf('.');

        if (dot === -1) {
            return `${plain}.0`;
        }

        const trimmed = plain.slice(dot + 1).replace(/0+$/, '');
        return `${plain.slice(0, dot)}.${trimmed || '0'}`;
    }

    negate(): Decimal {
        return this.coeff === 0n ? new Decimal(1, 0n, this.scale) : new Decimal(-this.sign as DecimalSign, this.coeff, this.scale);
    }

    add(other: Decimal): Decimal {
        if (this.coeff === 0n && this.scale <= other.scale) {
            return other;
        }

        if (other.coeff === 0n && other.scale <= this.scale) {
            return this;
        }

        const { left, right, scale } = align(this, other);

        if (this.sign === other.sign) {
            return fit(this.sign, left + right, scale);
        }

        if (left === right) {
            return new Decimal(1, 0n, scale);
        }

        if (left > right) {
            return fit(this.sign, left - right, scale);
        }

        return fit(other.sign, right - left, scale);
    }

    subtract(other: Decimal): Decimal {
        return this.add(other.negate());
    }

    multiply(other: Decimal): Decimal {
        const sign = (this.sign * other.sign) as DecimalSign;
        return fit(sign, this.coeff * other.coeff, this.scale + other.scale);
    }

    /**
     * .NET decimal division: exact when the remainder is zero, otherwise enough digits to fill a
     * 96-bit coefficient without exceeding scale 28. Midpoints use banker's rounding.
     */
    divide(other: Decimal): Decimal {
        if (other.coeff === 0n) {
            throw new Error('Division by zero.');
        }

        if (this.coeff === 0n) {
            return Decimal.zero();
        }

        const sign = (this.sign * other.sign) as DecimalSign;
        let quotient: bigint = this.coeff / other.coeff;
        let remainder: bigint = this.coeff % other.coeff;
        let extra = 0;

        const resultScale = () => extra + this.scale - other.scale;
        const canGrow = () => quotient <= MAX_COEFF / 10n;

        while (remainder !== 0n && canGrow() && resultScale() < MAX_SCALE) {
            remainder *= 10n;
            quotient = quotient * 10n + remainder / other.coeff;
            remainder = remainder % other.coeff;
            extra++;
        }

        if (remainder !== 0n) {
            const twice = remainder * 2n;
            const roundUp = twice > other.coeff || (twice === other.coeff && (quotient & 1n) === 1n);
            if (roundUp) {
                quotient += 1n;
            }
        }

        let scale = resultScale();

        if (remainder === 0n) {
            while (scale < 0) {
                if (quotient > MAX_COEFF / 10n) {
                    throw new DecimalOverflowError();
                }

                quotient *= 10n;
                scale++;
            }
        } else if (scale < 0) {
            throw new DecimalOverflowError();
        }

        return fit(sign, quotient, scale);
    }

    /**
     * Remainder with the sign of the dividend, truncated toward zero - `a - Truncate(a/b)*b`.
     */
    remainder(other: Decimal): Decimal {
        if (other.coeff === 0n) {
            throw new Error('Division by zero.');
        }

        return this.subtract(this.divide(other).truncate().multiply(other));
    }

    truncate(): Decimal {
        if (this.scale === 0) {
            return this;
        }

        return new Decimal(this.sign, this.coeff / 10n ** BigInt(this.scale), 0);
    }

    /**
     * Numeric comparison, ignoring scale. `9` and `9.0` compare equal, which is what `ef_compare`
     * and the `EF_DECIMAL` collation both need.
     */
    compareTo(other: Decimal): -1 | 0 | 1 {
        if (this.coeff === 0n && other.coeff === 0n) {
            return 0;
        }

        if (this.sign !== other.sign) {
            return this.sign < other.sign ? -1 : 1;
        }

        const { left, right } = align(this, other);
        if (left === right) {
            return 0;
        }

        const magnitude = left < right ? -1 : 1;
        return (this.sign < 0 ? -magnitude : magnitude) as -1 | 1;
    }

    static compare(left: Decimal, right: Decimal): -1 | 0 | 1 {
        return left.compareTo(right);
    }
}

function align(a: Decimal, b: Decimal): { left: bigint; right: bigint; scale: number } {
    const scale = Math.max(a.scale, b.scale);
    return {
        left: a.coeff * 10n ** BigInt(scale - a.scale),
        right: b.coeff * 10n ** BigInt(scale - b.scale),
        scale,
    };
}

/**
 * Fits a coefficient into 96 bits and a scale of 0–28, rounding with banker's rounding when digits
 * have to be dropped.
 *
 * All the surplus digits go in one division, and the whole discarded remainder decides the
 * rounding - which is what System.Decimal does. Dropping them one at a time and rounding at each
 * step is not the same thing: `…51` dropped digit by digit rounds down (1 is below half, then 5
 * ties to even), while the true remainder 0.51 is above half and rounds up.
 */
function fit(sign: DecimalSign, coeff: bigint, scale: number): Decimal {
    if (coeff < 0n) {
        throw new Error('fit() expects an unsigned coefficient.');
    }

    let drop = Math.max(0, scale - MAX_SCALE);
    while (coeff / 10n ** BigInt(drop) > MAX_COEFF) {
        drop++;
    }

    if (drop > scale) {
        throw new DecimalOverflowError();
    }

    if (drop > 0) {
        const divisor = 10n ** BigInt(drop);
        const remainder = coeff % divisor;
        coeff /= divisor;
        scale -= drop;

        const twice = remainder * 2n;
        if (twice > divisor || (twice === divisor && (coeff & 1n) === 1n)) {
            coeff += 1n;

            // Rounding up from the largest 96-bit coefficient carries out of it: 2^96 has to lose one
            // more digit, and its own remainder (…6) rounds that up too - the same as System.Decimal.
            if (coeff > MAX_COEFF) {
                if (scale === 0) {
                    throw new DecimalOverflowError();
                }

                coeff = coeff / 10n + 1n;
                scale--;
            }
        }
    }

    if (scale < 0) {
        throw new DecimalOverflowError();
    }

    return new Decimal(sign, coeff, scale);
}
