class LoggerTests {
    @Foundation.Core.Log()
    public static logException(): void {
        const obj = null;
        alert(obj.null);
    }
}