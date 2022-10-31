**Overview**

Iac project creates a prod environment using [pulumi](https://www.pulumi.com/)

The Prod environment has SQL server database, app insights, and app service.

**Getting started:**

1-  Run the following commands using [azure cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli):

```
az provider register --namespace 'Microsoft.Network'

az provider register --namespace 'Microsoft.Compute'

az provider register --namespace 'Microsoft.OperationalInsights'

az provider register --namespace 'Microsoft.Web'

az provider register --namespace 'Microsoft.Sql'

az provider register --namespace 'Microsoft.Insights'

az provider register --namespace 'Microsoft.KeyVault'

```

2- Create td-prod resource group

```
az group create --name td-prod --location eastus
```

Notes:
* `td` is an abbreviation for Todo, use the acronym of your choice and replace td with that (for example abc) using exact match - case sensitive find and replace in this file and TdStack.cs.
* You can use any location supported by azure cloud (run `az account list-locations -o table` to see full list of locations)

3- Create [service principals](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals) for prod using followings:

```
az ad sp create-for-rbac -n "td-prod" --role Contributor --scopes /subscriptions/{subscriptionId}/resourceGroups/td-prod
```

Notes:

* Replace `{subscriptionId}` with [your own subscription id](https://docs.microsoft.com/en-us/azure/media-services/latest/setup-azure-subscription-how-to)
* Running `az ad sp` will return a json like response that contains `appId`l, `password` and `tenant`. Store them somewhere safe.

4- Create the stacks folder first, then create `prod` folder in the `stacks` folder.

5- Create random/strong password for `prod` pulumi stack and store it somewhere safe.

6- Set `prod` stack's password in environment variables:

Windows's cmd sample:

```cmd
set PULUMI_CONFIG_PASSPHRASE=YOUR_PASSWORD
```

Windows PowerShell sample:

```powershell
$env:PULUMI_CONFIG_PASSPHRASE = 'YOUR_PASSWORD'
```

7- Run following to create `prod` stack:
```
pulumi login file://.\Stacks\prod
pulumi stack init prod
```

8- Provide valid configs and secrets for the first time for prod:

```
# clientId would be the service principal's appId.
pulumi config set azure-native:clientId 

# secret would be the service principal's password.
pulumi config set azure-native:clientSecret --secret

# tenantId would be the service principal's tenant.
pulumi config set azure-native:tenantId 

# Provide azure subscription id
pulumi config set azure-native:subscriptionId

# Provide SQL server's admin user/pass
pulumi config set TodoTemplate.Iac:sql-server-td-db-admin-id
pulumi config set TodoTemplate.Iac:sql-server-td-db-admin-password --secret

# Provide SMTP server's host, port, user, pass and default email sender.
pulumi config set TodoTemplate.Iac:default-email-from
pulumi config set TodoTemplate.Iac:email-server-host
pulumi config set TodoTemplate.Iac:email-server-port
pulumi config set TodoTemplate.Iac:email-server-userName
pulumi config set TodoTemplate.Iac:email-server-password --secret

# Create and provide an identity certificate password
pulumi config set TodoTemplate.Iac:identity-certificate-password --secret
```

9- Create azure resources using:
```
#  --skip-preview is required for the first run in each stack
pulumi up --stack prod --skip-preview
```

11- Commit / Push changes in the stacks folder to the source controller.