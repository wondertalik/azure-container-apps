@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('CosmosDB account name')
param cosmosDbAccountName string = '${projectName}-${targetEnvironment}-cosmos'

@description('CosmosDB database name')
param cosmosDbDatabaseName string = 'users-db'

@description('Use serverless capacity mode (recommended for dev/test)')
param serverless bool = true

@description('Provisioned throughput in RU/s — used only when serverless is false')
param throughput int = 400

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: serverless ? [{ name: 'EnableServerless' }] : []
    enableFreeTier: false
    disableLocalAuth: false
  }
  tags: {
    project: projectName
    environment: targetEnvironment
  }
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: cosmosDbAccount
  name: cosmosDbDatabaseName
  properties: {
    resource: {
      id: cosmosDbDatabaseName
    }
    options: serverless ? {} : {
      throughput: throughput
    }
  }
}

output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbDatabaseName string = cosmosDbDatabase.name
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
