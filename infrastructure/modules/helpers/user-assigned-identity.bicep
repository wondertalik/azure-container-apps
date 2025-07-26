@description('Name of the project related resources')
param projectName string

@description('Location of newly created resources')
param location string

@description('Target environment')
param targetEnvironment string

@description('Prefix of user assigned identity')
param namePrefix string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: 'id-${namePrefix}-${location}-${projectName}-${targetEnvironment}'
  location: location
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
  }
}

output identityId string = identity.id
output identityName string = identity.name
output identityPrincipalId string = identity.properties.principalId
