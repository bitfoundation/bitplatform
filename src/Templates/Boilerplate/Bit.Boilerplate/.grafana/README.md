# Azure Application Insights - Grafana Dashboard Boilerplate

This repository contains a production-ready Grafana Dashboard Boilerplate tailored for applications integrated with **Azure Application Insights (Log Analytics Workspace)**. It provides deep visibility into application health, performance metrics, and system exceptions using Kusto Query Language (KQL).

Check out read-only demo dashboard here: [Demo](https://cordialwombat2006.grafana.net/dashboard/snapshot/7Jop0tHLzy1etgeepKES3fNOk7KH39IH)
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

# FIX: Changed "Reader" to "Log Analytics Reader" to restrict access strictly to monitoring data
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
