module Bit.Implementations {
    export class DefaultMath implements Contracts.IMath {

        public sum(firstValue: string, secondValue: string): string {
            return Decimal.add(firstValue, secondValue).toString();
        }

        public multiply(firstValue: string, secondValue: string): string {
            return Decimal.mul(firstValue, secondValue).toString();
        }

        public divide(firstValue: string, secondValue: string): string {
            return Decimal.div(firstValue, secondValue).toString();
        }

        public subtract(firstValue: string, secondValue: string): string {
            return Decimal.sub(firstValue, secondValue).toString();
        }

        public toFixed(value: string, size: number = 2): string {
            return new Decimal(value).toFixed(size);
        }
    }
}