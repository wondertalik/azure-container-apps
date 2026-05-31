@description('Location of newly created resources')
param location string

@maxLength(20)
@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Azure Container Registry Name')
param azureContainerRegistryName string

@description('Azure Container Registry resource group name')
param azureContainerRegistryResourceGroupName string

// HttpApi
@description('Image name of the container app')
param httpApiContainerAppName string = ''

@description('Full image name of the container app')
param httpApiContainerAppImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('CPU resources')
param httpApiContainerAppResourcesCpu string = '0.5'

@description('Memory resources')
param httpApiContainerAppResourcesMemory string = '1Gi'

@description('Enable HttpApi container app')
param enableHttpApiContainerAppImage bool = false

@description('HttpApi scale min replicas')
param httpApiContainerAppScaleMinReplicas int = 0

@description('HttpApi scale max replicas')
param httpApiContainerAppScaleMaxReplicas int = 3

@description('Enable health probes for the container app')
param enableHttpApiContainerAppHealthProbes bool = false

// CosmosDB
// CosmosDB
@description('Enable CosmosDB account creation')
param enableCosmosDb bool = false

@description('CosmosDB account name')
param cosmosDbAccountName string = ''

@description('Use serverless capacity mode for CosmosDB')
param cosmosDbServerless bool = true

@description('Enable free tier for CosmosDB account (limit: 1 per subscription)')
param cosmosDbEnableFreeTier bool = false

// Users module
@description('CosmosDB database name for Users module')
param usersCosmosDbDatabaseName string = 'users-db'

@description('Enable Users init container job')
param enableUsersModule bool = false

@description('Full image reference for Users.InitContainer job')
param usersInitContainerImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('Whether to seed tenants during Users init')
param usersInitTenantsSeed bool = false

@description('Whether to seed users during Users init')
param usersInitUsersSeed bool = false

// FunctionApp1
@description('Full image name of the azure function container app')
param functionApp1Image string = 'mcr.microsoft.com/azure-functions/dotnet8-quickstart-demo:1.0'

@description('CPU resources')
param functionApp1ResourcesCpu string = '0.5'

@description('Memory resources')
param functionApp1ResourcesMemory string = '1Gi'

@description('Enable azure function container app')
param enableFunctionApp1Image bool = false

@description('Minimum number of instances that the function app can scale in to')
param functionApp1MinimumElasticInstanceCount int = 0

@description('Maximum number of instances that the function app can scale out to')
param functionApp1ScaleLimit int = 3

@description('Prefix for resource naming')
param namePrefix string = '01'

@description('Daily quota in GB for Log Analytics workspace (-1 for unlimited)')
param dailyQuotaGb string = '-1'

module telemetry './modules/telemetry.bicep' = {
  name: 'telemetry'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: namePrefix
    dailyQuotaGb: dailyQuotaGb
  }
}

module keyVault './modules/helpers/keyvault-helper.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: namePrefix
  }
}

module userAssignIdentity './modules/helpers/user-assigned-identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: namePrefix
  }
  dependsOn: [keyVault]
}

module azureContainerRegistry './modules/azure-container-registry.bicep' = {
  name: 'azureContainerRegistry'
  params: {
    azureContainerRegistryName: azureContainerRegistryName
    userAssignedIdentityName: userAssignIdentity.outputs.identityName
    userIdentityResourceGroupName: resourceGroup().name
  }
  scope: resourceGroup(azureContainerRegistryResourceGroupName)
}

module applicationContainerAppsEnvironment './modules/azure-container-apps-environment.bicep' = {
  name: 'applicationContainerAppsEnvironment'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    logAnalyticsWorkspaceName: telemetry.outputs.logAnalyticsWorkspaceName
    applicationInsightsName: telemetry.outputs.applicationInsightsName
    namePrefix: namePrefix
  }
}

module functionApp1 './modules/helpers/azure-function-container-app-helper.bicep' = if (enableFunctionApp1Image) {
  name: 'functionApp1'
  params: {
    location: location
    applicationInsightsName: telemetry.outputs.applicationInsightsName
    azureFunctionName: 'app1'
    containerAppsEnvironmentName: applicationContainerAppsEnvironment.outputs.containerAppsEnvironmentName
    projectName: projectName
    targetEnvironment: targetEnvironment
    userAssignedIdentityName: userAssignIdentity.outputs.identityName
    keyVaultName: keyVault.outputs.keyVaultName
    azureContainerRegistryName: azureContainerRegistryName
    alias: 'FunctionApp1'
    azureFunctionContainerAppImage: functionApp1Image
    resourcesCpu: functionApp1ResourcesCpu
    resourcesMemory: functionApp1ResourcesMemory
    minimumElasticInstanceCount: functionApp1MinimumElasticInstanceCount
    functionAppScaleLimit: functionApp1ScaleLimit
  }
}

module httpApiContainerApp './modules/helpers/azure-container-app-helper.bicep' = if (enableHttpApiContainerAppImage) {
  name: 'httpApiContainerApp'
  params: {
    location: location
    projectName: projectName
    alias: 'HttpApi'
    targetEnvironment: targetEnvironment
    containerAppsEnvironmentName: applicationContainerAppsEnvironment.outputs.containerAppsEnvironmentName
    userAssignedIdentityName: userAssignIdentity.outputs.identityName
    azureContainerRegistryName: azureContainerRegistryName
    containerAppName: httpApiContainerAppName
    containerAppImage: httpApiContainerAppImage
    resourcesCpu: httpApiContainerAppResourcesCpu
    resourcesMemory: httpApiContainerAppResourcesMemory
    scaleMinReplicas: httpApiContainerAppScaleMinReplicas
    scaleMaxReplicas: httpApiContainerAppScaleMaxReplicas
    enableHealthProbes: enableHttpApiContainerAppHealthProbes
  }
  dependsOn: [keyVault]
}

// CosmosDB account (shared infrastructure)
module cosmosDbAccount './modules/cosmosdb-account.bicep' = if (enableCosmosDb) {
  name: 'cosmosDbAccount'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    cosmosDbAccountName: cosmosDbAccountName
    serverless: cosmosDbServerless
    enableFreeTier: cosmosDbEnableFreeTier
  }
}

// Users module: database
module usersCosmosDbDatabase './modules/helpers/cosmosdb-sql-database.bicep' = if (enableCosmosDb && enableUsersModule) {
  name: 'usersCosmosDbDatabase'
  params: {
    cosmosDbAccountName: cosmosDbAccount!.outputs.cosmosDbAccountName
    databaseName: usersCosmosDbDatabaseName
    serverless: cosmosDbServerless
  }
}

// Users module: grant Managed Identity Cosmos DB Built-in Data Contributor
module usersCosmosDbRoleAssignment './modules/helpers/cosmosdb-role-assignment.bicep' = if (enableCosmosDb && enableUsersModule) {
  name: 'usersCosmosDbRoleAssignment'
  params: {
    cosmosDbAccountName: cosmosDbAccount!.outputs.cosmosDbAccountName
    principalId: userAssignIdentity.outputs.identityPrincipalId
  }
}

// Users module: InitContainer job (Manual trigger, run by pipeline)
module usersInitContainerJob './modules/helpers/init-container-job.bicep' = if (enableCosmosDb && enableUsersModule) {
  name: 'usersInitContainerJob'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    containerAppsEnvironmentName: applicationContainerAppsEnvironment.outputs.containerAppsEnvironmentName
    userAssignedIdentityName: userAssignIdentity.outputs.identityName
    azureContainerRegistryName: azureContainerRegistryName
    initContainerImage: usersInitContainerImage
    cosmosDbEndpoint: cosmosDbAccount!.outputs.cosmosDbEndpoint
    cosmosDbDatabaseName: usersCosmosDbDatabaseName
    tenantsSeed: usersInitTenantsSeed
    usersSeed: usersInitUsersSeed
  }
  dependsOn: [usersCosmosDbDatabase, usersCosmosDbRoleAssignment]
}
