// =============================================================================
// $0 container deployment — Azure Container Apps + GitHub Container Registry
//
// Why this costs nothing under normal demo/low-traffic use:
//   - Container App runs on the Consumption plan with scale-to-zero
//     (minReplicas: 0). No traffic => no replicas => no vCPU/memory billed.
//     Azure's monthly free grant (180,000 vCPU-seconds, 360,000 GiB-seconds
//     and 2,000,000 requests) then covers light real usage.
//   - The image lives in GitHub Container Registry (ghcr.io), which is free
//     for the repository — so we avoid Azure Container Registry entirely
//     (ACR has NO free tier; even Basic is ~$5/month).
//   - Log Analytics uses the PerGB2018 plan: first 5 GB/month free, plus a
//     hard 0.5 GB/day cap so a log spike can never start billing.
//   - No Azure SQL is provisioned by default (sqlConnectionString empty).
//
// Deploy:
//   az deployment group create -g <rg> -f infra/free/main.bicep \
//     -p environmentName=claims-fnol-free \
//        containerImage=ghcr.io/<owner>/<repo>:<tag>
// =============================================================================

targetScope = 'resourceGroup'

@description('Short name prefix used to derive resource names.')
@minLength(2)
@maxLength(20)
param environmentName string

@description('Azure region for every resource.')
param location string = resourceGroup().location

@description('Full container image reference, e.g. ghcr.io/owner/repo:sha. Must be publicly pullable for credential-free scale-from-zero.')
param containerImage string

@description('Port the container listens on.')
param targetPort int = 8080

@description('Optional SQL connection string. Leave empty to run the container without a database.')
@secure()
param sqlConnectionString string = ''

var tags = {
  'azd-env-name': environmentName
  application: 'claims-module-fnol'
  costProfile: 'free-tier'
}

// ---------- Log Analytics (free 5 GB/month, capped) --------------------------

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'law-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018' // first 5 GB/month free
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: json('0.5') // hard cap: a runaway log spike can never bill us
    }
  }
}

// ---------- Container Apps environment (Consumption — no fixed cost) ---------

resource cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${environmentName}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

// ---------- Container App (scale-to-zero) ------------------------------------

// Only wire the DB env var + secret when a connection string was supplied.
var dbEnv = empty(sqlConnectionString) ? [] : [
  {
    name: 'ConnectionStrings__ClaimsDb'
    secretRef: 'sql-connection'
  }
]
var appSecrets = empty(sqlConnectionString) ? [] : [
  {
    name: 'sql-connection'
    value: sqlConnectionString
  }
]

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-${environmentName}'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      // No `registries` block: the image is pulled anonymously from public ghcr.io,
      // so there are no long-lived registry credentials to store or expire.
      secrets: appSecrets
      ingress: {
        external: true
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      containers: [
        {
          name: 'app'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat([
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'Storage__Provider', value: 'LocalFileSystem' }
            { name: 'Storage__LocalRootPath', value: '/app/App_Data/uploads' }
          ], dbEnv)
        }
      ]
      scale: {
        // Scale-to-zero is what keeps idle cost at exactly $0.
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// ---------- Outputs ----------------------------------------------------------

output appUrl string                = 'https://${app.properties.configuration.ingress.fqdn}'
output containerAppName string      = app.name
output resourceGroup string         = resourceGroup().name
