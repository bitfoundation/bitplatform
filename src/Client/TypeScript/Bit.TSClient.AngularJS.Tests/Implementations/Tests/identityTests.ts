async function testAcrValuesAndIdentityServerLoggingTogether(): Promise<void> {

    await new Bit.Implementations.DefaultSecurityService()
        .loginWithCredentials("+9891255447788", "سلام به معنی Hello است", "TestResOwner", "secret", [{ key: "x", value: "1:1" }, { key: "y", value: "test test:test" }]);

}