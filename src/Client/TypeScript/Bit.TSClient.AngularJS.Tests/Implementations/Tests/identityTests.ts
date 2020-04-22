async function testAcrValuesAndIdentityServerLoggingTogether(): Promise<void> {

    await new Bit.Implementations.DefaultSecurityService()
        .loginWithCredentials("InValidUser", "InvalidPassword", "TestResOwner", "secret", [{ key: "x", value: "1" }, { key: "y", value: "2" }]);

}