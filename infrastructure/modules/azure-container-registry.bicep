@description('Azure Container Registry Name')
param azureContainerRegistryName string

@description('User Assigned Identity Name')
param userAssignedIdentityName string

@maxLength(30)
@description('Name of resouce group')
param userIdentityResourceGroupName string

resource azureContainerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: azureContainerRegistryName
}

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: userAssignedIdentityName
  scope: resourceGroup(userIdentityResourceGroupName)
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(userAssignedIdentity.id, 'AcrPullTestUserAssigned')
  scope: azureContainerRegistry
  properties: {
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    // acrPullDefinitionId has a value of 7f951dda-4ed3-4680-a7ca-43fe172d538d
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}
