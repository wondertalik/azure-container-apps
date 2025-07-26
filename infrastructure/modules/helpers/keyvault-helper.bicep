@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Prefix of key vault')
param namePrefix string

var tenantId = subscription().tenantId

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: 'kv-${namePrefix}-${projectName}-${targetEnvironment}' //A vault's name must be between 3-24 alphanumeric characters
  location: location
  properties: {
    tenantId: tenantId
    sku: {
        family: 'A'
        name: 'standard'
    }
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: true
    provisioningState: 'Succeeded'
  }
}

output keyVaultName string = keyVault.name
