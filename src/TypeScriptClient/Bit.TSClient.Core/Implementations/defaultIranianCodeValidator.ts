module Bit.Implementations {

    export class DefaultIranianCodeValidator implements Contracts.IIranianCodeValidator {

        @Log()
        public nationalCodeIsValid(code: string): boolean {

            if (code == null)
                return false;

            if (!/^\d{10}$/.test(code))
                return false;

            const check = parseInt(code[9]);

            const sum = [0, 1, 2, 3, 4, 5, 6, 7, 8]
                .map((x) => { return parseInt(code[x]) * (10 - x); })
                .reduce((x, y) => { return x + y; }) % 11;

            return sum < 2 && check == sum || sum >= 2 && check + sum == 11;

        }

        @Log()
        public companyCodeIsValid(companyCode: string): boolean {

            let result = (/^\d{11}$/).test(companyCode);

            if (result) {

                const invalidCompanyCodes = ["00000000000", "11111111111", "22222222222", "33333333333", "44444444444",
                    "55555555555", "66666666666", "77777777777", "88888888888", "99999999999"];

                result = !invalidCompanyCodes.includes(companyCode);

                if (result) {
                    const c = parseInt(companyCode[10]);
                    const c10 = parseInt(companyCode[9]);

                    const n = (parseInt(companyCode[0]) + c10 + 2) * 29 +
                        (parseInt(companyCode[1]) + c10 + 2) * 27 +
                        (parseInt(companyCode[2]) + c10 + 2) * 23 +
                        (parseInt(companyCode[3]) + c10 + 2) * 19 +
                        (parseInt(companyCode[4]) + c10 + 2) * 17 +
                        (parseInt(companyCode[5]) + c10 + 2) * 29 +
                        (parseInt(companyCode[6]) + c10 + 2) * 27 +
                        (parseInt(companyCode[7]) + c10 + 2) * 23 +
                        (parseInt(companyCode[8]) + c10 + 2) * 19 +
                        (parseInt(companyCode[9]) + c10 + 2) * 17;

                    let r = n - (Math.floor((n / 11)) * 11);

                    if (r === 10)
                        r = 0;

                    if (r === c)
                        result = true;
                    else
                        result = false;
                }

            }

            return result;
        }
    }
}