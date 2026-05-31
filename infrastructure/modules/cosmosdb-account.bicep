@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('CosmosDB account name')
param cosmosDbAccountName string = '${projectName}-${targetEnvironment}-cosmos'

@description('Use serverless capacity mode (recommended for dev/test)')
param serverless bool = true

@description('Enable free tier for this CosmosDB account (limit: 1 per subscription)')
param enableFreeTier bool = false

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: serverless ? [{ name: 'EnableServerless' }] : []
    enableFreeTier: enableFreeTier
    enableAutomaticFailover: !serverless
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    disableLocalAuth: false
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous7Days'
      }
    }
  }
  tags: {
    project: projectName
    environment: targetEnvironment
  }
}

output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
