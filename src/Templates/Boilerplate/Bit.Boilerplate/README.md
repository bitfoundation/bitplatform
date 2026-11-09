## Welcome!

Thank you for creating a new project with bit platform! We appreciate your trust in our platform and are excited to see what you'll build.

## Seeded accounts

So that the app is usable and testable the moment it is generated, the database seed
(`src/Server/Boilerplate.Server.Api/Features/Identity/Configurations/UserConfiguration.cs`) creates
pre-confirmed accounts whose password is **`12345678`**:

| E-mail | Password | Role |
|---|---|---|
| `test@bitplatform.dev` | `12345678` | `g-admin` - global administrator |
<!--#if (multitenant == true)-->
| `store-admin@bitplatform.dev` | `12345678` | `t-admin` of the seeded `store` tenant |
| `store-user@bitplatform.dev` | `12345678` | `demo` member of the seeded `store` tenant |
<!--#endif-->

These passwords are for development and are not for Production: the first deployment still creates the accounts, so
change the passwords right after it.


## Documentation

To help you get started, we have comprehensive documentation available. You can find detailed guides, samples, and more at:

[bitplatform.dev/templates](https://bitplatform.dev/templates/overview)

If you have any questions or need further assistance, feel free to [reach out](https://github.com/bitfoundation/bitplatform/issues/new/choose). Happy coding!

The **bit platform** team.
