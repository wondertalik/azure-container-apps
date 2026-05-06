@description('CosmosDB account name')
param cosmosDbAccountName string

@description('Principal ID of the identity to grant access')
param principalId string

@description('Role definition ID — defaults to Cosmos DB Built-in Data Contributor')
// Built-in Data Contributor: 00000000-0000-0000-0000-000000000002
param roleDefinitionId string = '00000000-0000-0000-0000-000000000002'

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = {
  name: cosmosDbAccountName
}

resource cosmosDbRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = {
  parent: cosmosDbAccount
  name: guid(cosmosDbAccount.id, principalId, roleDefinitionId)
  properties: {
    roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/${roleDefinitionId}'
    principalId: principalId
    scope: cosmosDbAccount.id
  }
}
