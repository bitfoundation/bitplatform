import { test, expect } from 'vitest';
import { Decimal } from '../../Bit.Bsqlite/Scripts/bit-bsqlite-decimal.ts';

const values = ['0', '10', '9', '-5', '2.5', '0.1', '100', '-0.25', '1234.5678', '3'];
const optional = [null, '10', null, '-5', '2.5', null, '100', '-0.25', '1234.5678', '3'];

const parse = text => Decimal.parse(text);
const map = (xs, fn) => xs.map(x => (x === null ? null : fn(parse(x)).toString()));

test('ToString keeps the scale, including trailing zeros', () => {
    expect(parse('2.5').add(parse('1.5')).toString()).toBe('4.0');
    expect(parse('0').add(parse('1.5')).toString()).toBe('1.5');
    expect(parse('0.1').multiply(parse('10')).toString()).toBe('1.0');
    expect(parse('9.0').toString()).toBe('9.0');
    expect(parse('9').toString()).toBe('9');
    expect(parse('.5').toString()).toBe('0.5');
    expect(parse('5.').toString()).toBe('5');
    expect(parse('-0').toString()).toBe('0');
});

// The oracle is Microsoft.Data.Sqlite's `0.0###########################`, which is the form EF
// renders decimal literals in and therefore the only form its `=` comparisons match.
test('toSqlText writes the canonical form the SQLite stack stores', () => {
    expect(parse('10').toSqlText()).toBe('10.0');
    expect(parse('1.50').toSqlText()).toBe('1.5');
    expect(parse('100.000').toSqlText()).toBe('100.0');
    expect(parse('0').toSqlText()).toBe('0.0');
    expect(parse('-3').toSqlText()).toBe('-3.0');
    expect(parse('12.34').toSqlText()).toBe('12.34');
    expect(parse('-0.25').toSqlText()).toBe('-0.25');
    expect(parse('1').divide(parse('3')).toSqlText()).toBe('0.3333333333333333333333333333');
    expect(parse('2.5').add(parse('1.5')).toSqlText()).toBe('4.0');
});

test('add matches the S4 oracle', () => {
    expect(map(values, v => v.add(parse('1.5')))).toEqual(['1.5', '11.5', '10.5', '-3.5', '4.0', '1.6', '101.5', '1.25', '1236.0678', '4.5']);
});

test('subtract matches the S4 oracle', () => {
    expect(map(values, v => v.subtract(parse('1.5')))).toEqual(['-1.5', '8.5', '7.5', '-6.5', '1.0', '-1.4', '98.5', '-1.75', '1233.0678', '1.5']);
});

test('multiply matches the S4 oracle', () => {
    expect(map(values, v => v.multiply(parse('3')))).toEqual(['0', '30', '27', '-15', '7.5', '0.3', '300', '-0.75', '3703.7034', '9']);
});

// When a product has more than 28 decimals, every surplus digit is dropped at once and the whole
// remainder decides the rounding, as System.Decimal does. Rounding digit by digit gets `…51` wrong:
// 1 is below half, then 5 ties to even, and 0.51 rounds down instead of up. Oracle: .NET 10.
test('multiply drops surplus digits with one rounding, not one per digit', () => {
    const tiny = parse('0.0000000000000000000000000001');
    const cases = [
        ['0.51', '0.0000000000000000000000000001'],
        ['0.49', '0.0000000000000000000000000000'],
        ['1.50', '0.0000000000000000000000000002'],
        ['2.50', '0.0000000000000000000000000002'],
    ];
    expect(cases.map(([factor]) => tiny.multiply(parse(factor)).toString())).toEqual(cases.map(([, expected]) => expected));

    const three = parse('0.0000000000000000000000000003');
    expect(three.multiply(parse('1.51')).toString()).toBe('0.0000000000000000000000000005');
    expect(three.multiply(parse('1.50')).toString()).toBe('0.0000000000000000000000000004');
});

test('divide matches the S4 oracle', () => {
    expect(map(values, v => v.divide(parse('4')))).toEqual(['0', '2.5', '2.25', '-1.25', '0.625', '0.025', '25', '-0.0625', '308.64195', '0.75']);
});

test('remainder matches the S4 oracle, including the sign of the dividend', () => {
    expect(map(values, v => v.remainder(parse('3')))).toEqual(['0', '1', '0', '-2', '2.5', '0.1', '1', '-0.25', '1.5678', '0']);
});

test('negate matches the S4 oracle and does not produce -0', () => {
    expect(map(values, v => v.negate())).toEqual(['0', '-10', '-9', '5', '-2.5', '-0.1', '-100', '0.25', '-1234.5678', '-3']);
});

test('1/3 is twenty-eight threes, not an IEEE float', () => {
    expect(parse('1').divide(parse('3')).toString()).toBe('0.3333333333333333333333333333');
});

test('10 / 0.04 is exact', () => {
    expect(parse('10').divide(parse('0.04')).toString()).toBe('250');
});

test('aggregates match the S4 oracle', () => {
    const sum = values.map(parse).reduce((a, b) => a.add(b));
    expect(sum.toString()).toBe('1353.9178');
    expect(sum.divide(parse('10')).toString()).toBe('135.39178');

    const present = optional.filter(v => v !== null).map(parse);
    const sumOpt = present.reduce((a, b) => a.add(b));
    expect(sumOpt.toString()).toBe('1344.8178');
    expect(sumOpt.divide(new Decimal(1, BigInt(present.length), 0)).toString()).toBe('192.11682857142857142857142857');

    const bucket0 = ['0', '10', '9', '-5'].map(parse).reduce((a, b) => a.add(b));
    const bucket1 = ['2.5', '0.1', '100', '-0.25', '1234.5678', '3'].map(parse).reduce((a, b) => a.add(b));
    expect(bucket0.toString()).toBe('14');
    expect(bucket0.divide(parse('4')).toString()).toBe('3.5');
    expect(bucket1.toString()).toBe('1339.9178');
    expect(bucket1.divide(parse('6')).toString()).toBe('223.31963333333333333333333333');
});

test('compare is numeric, so 9 is less than 10 and equal to 9.0', () => {
    expect(Decimal.compare(parse('9'), parse('10'))).toBe(-1);
    expect(Decimal.compare(parse('10'), parse('9'))).toBe(1);
    expect(Decimal.compare(parse('9'), parse('9.0'))).toBe(0);
    expect(Decimal.compare(parse('2.5'), parse('100'))).toBe(-1);
    expect(Decimal.compare(parse('-5'), parse('-0.25'))).toBe(-1);
    expect(Decimal.compare(parse('0'), parse('-0'))).toBe(0);
});

test('order of the S4 values is numeric, not lexicographic', () => {
    const ids = values
        .map((text, i) => ({ id: i + 1, value: parse(text) }))
        .sort((a, b) => a.value.compareTo(b.value) || a.id - b.id)
        .map(row => row.id);

    expect(ids).toEqual([4, 8, 1, 6, 5, 10, 3, 2, 7, 9]);
});
