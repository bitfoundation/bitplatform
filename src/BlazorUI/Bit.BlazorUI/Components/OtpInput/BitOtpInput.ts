class BitOtpInput {
    static setupOtpInputPaste(dotnetReference: DotNetObject, otpInput: HTMLElement) {

        otpInput.addEventListener('paste', async e => {
            e.preventDefault();
            let pastedValue = e.clipboardData?.getData('Text');
            await dotnetReference.invokeMethodAsync("SetPastedData", pastedValue);
        });
    }
}