module Bit.Contracts {
    export interface IIranianCodeValidator {
        nationalCodeIsValid(code: string): boolean;
        companyCodeIsValid(companyCode: string): boolean;
    }
}