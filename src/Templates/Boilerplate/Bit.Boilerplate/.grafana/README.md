# Azure Application Insights - Grafana Dashboard Boilerplate

> **Requires `--appInsights true`.** Every panel in `dashboard.json` is a KQL query against the Azure Monitor
> Log Analytics tables (`AppRequests`, `AppExceptions`, `AppDependencies`, `AppMetrics`), which are only written
> when the Azure Monitor exporter is compiled in. A project generated without `--appInsights true` - the default -
> exports OpenTelemetry over OTLP instead, so these panels will have no data source to point at. The folder is
> shipped anyway so you can adopt it later; it is inert until then.
>
> One panel has a second requirement: **Ongoing AI Conversations** reads `appHub.ongoing_conversations_count`,
> which only exists with `--signalR true`.

This folder contains a production-ready Grafana dashboard tailored for applications integrated with **Azure Application Insights (Log Analytics Workspace)**. It provides deep visibility into application health, performance metrics, and system exceptions using Kusto Query Language (KQL).

---

## 🚀 Key Features & Benefits

### 1. Advanced Exception Classification (Known vs. Unknown)
Unlike standard dashboards that lump all errors together, this boilerplate splits exceptions into clear, actionable categories:
* **Known & Transient Exceptions:** Handled business-rule validations or expected transient network errors (filtered via `customDimensions.KnownException == true` or specific error types). This keeps noise low for engineering teams.
* **Unknown Exceptions:** Unhandled crashes, critical panics, and unexpected failures that require immediate engineering triage and root-cause analysis.

### 2. Custom Business & Application Metrics
Track the real-world operational health of your application beyond infrastructure metrics. The dashboard includes pre-configured panels for custom Telemetry Metrics, such as:
* **Active Chat Sessions:** Live monitoring of concurrent ongoing chats.
* **Media Processing Pipeline:** Real-time throughput of uploaded and resized images.

### 3. Clean Performance Baselining (SignalR Optimization)
Standard server response time metrics are often heavily skewed by long-lived connection protocols. This dashboard **excludes `SignalR /AppHub` requests** from the main Server Response Time panels. Because WebSocket and Long-Polling connections remain open intentionally for long durations, removing them ensures your HTTP REST API response time percentiles (p50, p95, p99) reflect reality.

---

## 🔍 Quick Feature Overview & Panels

* **Exception Rate Tracker:** A time-series visualization showcasing the ratio and frequency of Handled vs. Unhandled errors.
* **Performance Metrics:** Server response times and dependency durations with automated SignalR filtering.
* **Custom Application Insights:** Dedicated counters and gauges for domain-specific events (Chats, Uploads).
* **Reliability Dashboard:** Overall availability percentages based on successful vs. failed operations.

---

## 🛠️ Azure Integration Setup

To connect this Grafana dashboard to your Azure Monitor / Application Insights data source, you need to configure a Service Principal with the `Log Analytics Reader` role. 

You can use the following PowerShell script to quickly provision the required Azure credentials (Tenant ID, Client ID, and Client Secret) and fetch the Workspace ID:

```ps1
# ----------------------------------------------------
# 0. PRE-FLIGHT AUTHENTICATION CHECK
# ----------------------------------------------------
$currentContext = Get-AzContext
if (-not $currentContext) {
    Write-Error "No Azure login detected! Please run 'Connect-AzAccount' first, then rerun this script."
    break
}

# ----------------------------------------------------
# 1. DEFINE VARIABLES
# ----------------------------------------------------
$appName = "Grafana-AzureMonitor-Integration"
$secretName = "GrafanaClientSecret"
$durationInYears = 2

# ----------------------------------------------------
# 2. CREATE THE ENTRA ID APP REGISTRATION & SP
# ----------------------------------------------------
Write-Host "Creating Entra ID App Registration: $appName..." -ForegroundColor Cyan

# Create the base Application
$azureAdApp = New-AzADApplication -DisplayName $appName

# Create the Service Principal linked to that application
$servicePrincipal = New-AzADServicePrincipal -ApplicationId $azureAdApp.AppId

# ----------------------------------------------------
# 3. GENERATE CLIENT SECRET
# ----------------------------------------------------
Write-Host "Generating Client Secret..." -ForegroundColor Cyan
$now = Get-Date

# Create the strongly typed Graph Password Credential object
$passwordObj = [Microsoft.Azure.PowerShell.Cmdlets.Resources.MSGraph.Models.ApiV10.MicrosoftGraphPasswordCredential]::new()
$passwordObj.DisplayName = $secretName
$passwordObj.EndDateTime = $now.AddYears($durationInYears)

# Pass it into the cmdlet as an array argument to -PasswordCredentials
$secretCredential = New-AzADAppCredential `
    -ApplicationId $azureAdApp.AppId `
    -PasswordCredentials @($passwordObj)

$clientSecretText = $secretCredential.SecretText

# ----------------------------------------------------
# 4. ASSIGN RBAC PERMISSIONS (LOG ANALYTICS READER ROLE)
# ----------------------------------------------------
$subscriptionId = $currentContext.Subscription.Id
$tenantId = $currentContext.Tenant.Id

# NOTE: "Log Analytics Reader" (not "Reader") restricts access strictly to monitoring data.
# The scope below is the whole subscription, which is what the dashboard's `subscriptions()` and
# `workspaces($subscription)` variables need in order to populate their dropdowns. If your organisation
# requires least privilege and you already know the workspace, narrow it to that resource id instead
# (-Scope $workspace.ResourceId) and set the dashboard's subscription/workspace variables to fixed values -
# the two go together, one without the other leaves you with empty dropdowns.
Write-Host "Assigning 'Log Analytics Reader' role to the Service Principal on Subscription: $subscriptionId..." -ForegroundColor Cyan

New-AzRoleAssignment `
    -ObjectId $servicePrincipal.Id `
    -RoleDefinitionName "Log Analytics Reader" `
    -Scope "/subscriptions/$subscriptionId"

# ----------------------------------------------------
# 5. OUTPUT CONFIGURATION FOR GRAFANA
# ----------------------------------------------------
Write-Host "`n====================================================" -ForegroundColor Green
Write-Host "SUCCESS! Copy these details into your Grafana UI:" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host "Authentication Method : App Registration (client secret)"
Write-Host "Directory (tenant) ID : $tenantId"
Write-Host "Application (client) ID: $($azureAdApp.AppId)"
Write-Host "Client Secret         : $clientSecretText"
Write-Host "Default Subscription  : $subscriptionId"
Write-Host "====================================================`n"

$yamlOutput = @"
apiVersion: 1

datasources:
  - name: Azure Monitor
    type: grafana-azure-monitor-datasource
    access: proxy
    jsonData:
      azureAuthType: clientsecret
      cloudName: azuremonitor
      tenantId: $tenantId
      clientId: $($azureAdApp.AppId)
      subscriptionId: $subscriptionId
    secureJsonData:
      clientSecret: $clientSecretText
    version: 1
"@

Write-Host "Or if you use Grafana Provisioning, save this snippet as a YAML file:" -ForegroundColor Cyan
Write-Host $yamlOutput -ForegroundColor Yellow
```
