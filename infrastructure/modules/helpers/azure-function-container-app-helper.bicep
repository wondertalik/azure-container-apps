@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Azure function app alias')
param alias string

@description('Azure function app name')
param azureFunctionName string

@description('Container apps environment')
param containerAppsEnvironmentName string

@description('Name of the application insights name resource')
param applicationInsightsName string

@description('Name of the user assigned identity resource')
param userAssignedIdentityName string

@description('Name of caropticom key vault')
param keyVaultName string

@description('Id of the user assigned id resource')
param azureContainerRegistryName string

@description('Full image name of the azure function app')
param azureFunctionContainerAppImage string = 'mcr.microsoft.com/azure-functions/dotnet8-quickstart-demo:1.0'

@description('Minimum number of instances that the function app can scale in to')
param minimumElasticInstanceCount int = 0

@description('Maximum number of instances that the function app can scale out to')
param functionAppScaleLimit int = 3

@description('CPU resources')
param resourcesCpu string

@description('Memory resources')
param resourcesMemory string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' existing = {
  name: containerAppsEnvironmentName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: userAssignedIdentityName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Replace hyphens with underscores
var stringWithoutHyphens = replace(azureFunctionName, '-', '')

// Convert the string to lowercase
var storageAccountName = toLower(stringWithoutHyphens)

resource azStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${storageAccountName}${targetEnvironment}' // 3 and 24 characters in length and use numbers and lower-case letters only.
  location: location
  kind: 'StorageV2'
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
    alias: alias
  }
  sku: {
    name: 'Standard_LRS'
  }
}

var functionAppName = 'func-${azureFunctionName}-${projectName}-${targetEnvironment}'
resource azfunctionapp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux,container,azurecontainerapps'
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
    alias: alias
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    resourceConfig: {
      cpu: json(resourcesCpu)
      memory: resourcesMemory
    }
    siteConfig: {
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: identity.id
      minimumElasticInstanceCount: minimumElasticInstanceCount
      functionAppScaleLimit: functionAppScaleLimit
      linuxFxVersion: 'Docker|${azureFunctionContainerAppImage}'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: azStorageAccount.name
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: '${azureContainerRegistryName}.azurecr.io'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
      ]
    }
  }
}

resource roleAssignmenStorageBlobDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(azStorageAccount.name, 'Storage Blob Data Owner User Function App')
  scope: azStorageAccount
  properties: {
    principalId: azfunctionapp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
  }
}

resource roleAssignmentKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(azfunctionapp.name, 'Key Vault Secrets User Function App')
  scope: keyVault
  properties: {
    principalId: azfunctionapp.identity.principalId
    principalType: 'ServicePrincipal'
    // Key Vault Secrets User has a value of 4633458b-17de-408a-b874-0445c86b69e6
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource roleAssignmentKeyVaultCertificateUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(azfunctionapp.name, 'Key Vault Certificate User Function App')
  scope: keyVault
  properties: {
    principalId: azfunctionapp.identity.principalId
    principalType: 'ServicePrincipal'
    // Key Vault Certificate User has a value of db79e9a7-68ee-4b58-9aeb-b90e7c24fcba
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'db79e9a7-68ee-4b58-9aeb-b90e7c24fcba')
  }
}

output functionAppName string = azfunctionapp.name
output azStorageAccountName string = azStorageAccount.name
