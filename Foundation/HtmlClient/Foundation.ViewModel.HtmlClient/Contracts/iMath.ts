module Foundation.ViewModel.Contracts {
    export interface IMath {
        sum(firstValue: string, secondValue: string): string;
        multiply(firstValue: string, secondValue: string): string;
        divide(firstValue: string, secondValue: string): string;
        subtract(firstValue: string, secondValue: string): string;
        toFixed(value: string, size: number): string;
    }
}
