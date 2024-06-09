namespace BitBlazorUI {
    export class OtpInput {
        public static setup(dotnetReference: DotNetObject, input: HTMLInputElement) {
            input.addEventListener('focus', (e: any) => {
                e.target?.select();
            });

            input.addEventListener('paste', async e => {
                e.preventDefault();
                let pastedValue = e.clipboardData?.getData('Text');
                await dotnetReference.invokeMethodAsync("SetPastedData", pastedValue);
            });
        }
    }
}