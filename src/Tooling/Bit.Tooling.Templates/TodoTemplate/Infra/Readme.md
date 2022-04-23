**Overview**

Infra projects creates two environments (test & prod) using [pulumi](https://www.pulumi.com/)

Each environment has sql server database, app insights, blob storage and app service.

This project also creates an azure dev ops agent vm which its ip is whitlisted in sql server's firewall rules so it can run migrations against database in every release we made.

**Getting started:**

1- Create three resource groups (`td-test`, `td-prod`, `td-cd`) using [azure cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli):

```
az group create --name td-test --location eastus

az group create --name td-prod --location eastus

az group create --name td-cd --location eastus
```

Notes:
* `td` stands for todo, you can replace it with your own.
* `td-cd` resource group contains azure dev ops vm agent related resources.
* You can use any location supported by azure cloud (run `az account list-locations -o table` to see full list of locations)

2- Create three [service principals](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals) for `test`, `prod` and `cd` using followings:

```
# Create service principal that manage test resource group resources:
az ad sp create-for-rbac -n "td-test" --role Contributor --scopes /subscriptions/{subscriptionId}/resourceGroups/td-test

# Create service principal that manage prod resource group resources:
az ad sp create-for-rbac -n "td-prod" --role Contributor --scopes /subscriptions/{subscriptionId}/resourceGroups/td-prod

# Create service principal that manage cd resource group resources:
az ad sp create-for-rbac -n "td-cd" --role Contributor --scopes /subscriptions/{subscriptionId}/resourceGroups/td-cd
```

Notes:

* Replace `{subscriptionId}` with [your own subscription id](https://docs.microsoft.com/en-us/azure/media-services/latest/setup-azure-subscription-how-to)
* Running `az ad sp` will return a json like response which contains `appId`l, `password` and `tenant`. Store them somewhere safe.

3- Create stacks folder first, then create `test`, `prod` and `cd` folders in `stacks` folder.

4- Create three different passwords for three pulumi stacks (`cd`, `test`, `prod`) and store them somewhere safe.

5- Set `cd` stack's password in environment variables:

Windows's cmd sample:

```cmd
set PULUMI_CONFIG_PASSPHRASE=YOUR_PASSWORD
```

Windows PowerShell sample:

```powershell
$env:PULUMI_CONFIG_PASSPHRASE = 'YOUR_PASSWORD'
```

6- Run followings to create `cd` stack:
```
pulumi login file://.\Stacks\cd
pulumi stack init cd
```

7- Provide valid configs and secrets for the first time for cd:

```
# clientId would be service principal's appId.
pulumi config set azure-native:clientId 

# secret would be service principal's password.
pulumi config set azure-native:clientSecret --secret

# tenantId would be service principal's tenant.
pulumi config set azure-native:tenantId 

# Provide azure subscription id
pulumi config set azure-native:subscriptionId

# Provide azure dev ops agent vm's admin userName.
pulumi config set TodoTemplate.Infra:dev-ops-vm-td-admin-user-name

# Create and provide azure dev ops agent vm's admin password: (Store it somewhere safe)
pulumi config set TodoTemplate.Infra:dev-ops-vm-td-admin-user-password --secret
```

8- Create azure resources using:
```
#  --skip-preview is required for the first run in each stack (cd, test, prod)
pulumi up --stack cd --skip-preview
```

9- Run steps (5 to 8) for test & prod but with different configs:

```
# Provide service principal's info:
pulumi config set azure-native:clientId 
pulumi config set azure-native:clientSecret --secret
pulumi config set azure-native:tenantId 
pulumi config set azure-native:subscriptionId 

# Provide sql server's admin user / pass
pulumi config set TodoTemplate.Infra:sql-server-td-db-admin-id
pulumi config set TodoTemplate.Infra:sql-server-td-db-admin-password --secret

# Provide sql server's non-admin user / pass
# This user is gets created during CI/CD and will have access to CRUD operations only. App service connects to the sql database using this user.
pulumi config set TodoTemplate.Infra:sql-server-td-db-user-id
pulumi config set TodoTemplate.Infra:sql-server-td-db-user-password --secret

# Provide smtp server's host, port, user, pass and default email sender.
pulumi config set TodoTemplate.Infra:default-email-from
pulumi config set TodoTemplate.Infra:email-server-host
pulumi config set TodoTemplate.Infra:email-server-port
pulumi config set TodoTemplate.Infra:email-server-userName
pulumi config set TodoTemplate.Infra:email-server-password --secret

# Create and provide jwt secret key
pulumi config set TodoTemplate.Infra:jwt-secret-key --secret

# Provide azure dev ops agent vm's IP address which gets created before
pulumi config set TodoTemplate.Infra:azure-dev-ops-agent-vm-ip
```

10- Commit / Push changes in stacks folder to the source controller.
