@maxLength(36)
@minLength(36)
@description('Azure SubscriptionId')
param subscriptionId string

@maxLength(30)
@description('Name of resouce group')
param applicationResourceGroupName string

@description('Location of newly created resources')
param location string

@maxLength(20)
@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Azure Container Registry Name')
param azureContainerRegistryName string

@description('Azure Container Registry restore group name')
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

targetScope = 'subscription'

module applicationResourceGroup './modules/resource-group.bicep' = {
  name: applicationResourceGroupName
  params: {
    location: location
    resourceGroupName: applicationResourceGroupName
  }
  scope: subscription(subscriptionId)
}

module telemetry './modules/telemetry.bicep' = {
  name: 'telemetry'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: '01'
  }
  scope: az.resourceGroup(applicationResourceGroup.name)
}

module keyVault './modules/helpers/keyvault-helper.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: '01'
  }
  scope: az.resourceGroup(applicationResourceGroup.name)
}

module userAssignIdentity './modules/helpers/user-assigned-identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    namePrefix: '01'
  }
  scope: az.resourceGroup(applicationResourceGroup.name)
  dependsOn: [keyVault]
}

module azureContainerRegistry './modules/azure-container-registry.bicep' = {
  name: 'azureContainerRegistry'
  params: {
    azureContainerRegistryName: azureContainerRegistryName
    userAssignedIdentityName: userAssignIdentity.outputs.identityName
    userIdentityResourceGroupName: applicationResourceGroup.name
  }
  scope: az.resourceGroup(azureContainerRegistryResourceGroupName)
}

module applicationContainerAppsEnvironment './modules/azure-container-apps-environment.bicep' = {
  name: 'applicationContainerAppsEnvironment'
  params: {
    location: location
    projectName: projectName
    targetEnvironment: targetEnvironment
    logAnalyticsWorkspaceName: telemetry.outputs.logAnalyticsWorkspaceName
    applicationInsightsName: telemetry.outputs.applicationInsightsName
    namePrefix: '01'
  }
  scope: az.resourceGroup(applicationResourceGroup.name)
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
  scope: az.resourceGroup(applicationResourceGroup.name)
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
  }
  scope: az.resourceGroup(applicationResourceGroup.name)
  dependsOn: [keyVault]
}
