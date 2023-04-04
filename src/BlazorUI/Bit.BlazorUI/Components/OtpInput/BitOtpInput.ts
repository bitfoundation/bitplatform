class BitOtpInput {
    static setupOtpInput(dotnetReference: DotNetObject, otpInput: HTMLInputElement) {

        otpInput.addEventListener('paste', async e => {
            e.preventDefault();
            let pastedValue = e.clipboardData?.getData('Text');
            await dotnetReference.invokeMethodAsync("SetPastedData", pastedValue);
        });
    }
}