class LoggerTests {
    @Bit.Log()
    public static logException(): void {
        const obj = null;
        alert(obj.null);
    }
}