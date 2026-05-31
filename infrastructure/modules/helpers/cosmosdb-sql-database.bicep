@description('CosmosDB account name')
param cosmosDbAccountName string

@description('Database name')
param databaseName string

@description('Use serverless capacity mode')
param serverless bool = true

@description('Provisioned throughput in RU/s — used only when serverless is false')
param throughput int = 400

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: cosmosDbAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
    options: serverless ? {} : {
      throughput: throughput
    }
  }
}

output databaseName string = cosmosDbDatabase.name
