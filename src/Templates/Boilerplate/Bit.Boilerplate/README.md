## Welcome!

Thank you for creating a new project with bit platform! We appreciate your trust in our platform and are excited to see what you'll build.

## ⚠️ Seeded accounts - remove them before you deploy

So that the app is usable and testable the moment it is generated, the database seed
(`src/Server/Boilerplate.Server.Api/Features/Identity/Configurations/UserConfiguration.cs`) creates real,
pre-confirmed accounts whose password is **`12345678`**:

| E-mail | Password | Role |
|---|---|---|
| `test@bitplatform.dev` | `12345678` | `g-admin` - **global administrator, holds every feature** |
<!--#if (multitenant == true)-->
| `store-admin@bitplatform.dev` | `12345678` | `t-admin` of the seeded `store` tenant |
| `store-user@bitplatform.dev` | `12345678` | `demo` member of the seeded `store` tenant |
<!--#endif-->

These credentials, their password hash and their primary keys are all part of the public bit platform template,
so **anyone** can sign in as a global administrator on a deployment that still carries them.


## Documentation

To help you get started, we have comprehensive documentation available. You can find detailed guides, samples, and more at:

[bitplatform.dev/templates](https://bitplatform.dev/templates/overview)

If you have any questions or need further assistance, feel free to [reach out](https://github.com/bitfoundation/bitplatform/issues/new/choose). Happy coding!

The **bit platform** team.
