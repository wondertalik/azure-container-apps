@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Container Apps environment name')
param containerAppsEnvironmentName string

@description('Name of the user-assigned identity')
param userAssignedIdentityName string

@description('Azure Container Registry name')
param azureContainerRegistryName string

@description('Full image reference for Users.InitContainer')
param initContainerImage string

@description('CosmosDB account endpoint')
param cosmosDbEndpoint string

@description('CosmosDB database name')
param cosmosDbDatabaseName string = 'users-db'

@description('Whether to seed tenants during init')
param tenantsSeed bool = false

@description('Whether to seed users during init')
param usersSeed bool = false

@description('CPU allocated to the job')
param resourcesCpu string = '0.5'

@description('Memory allocated to the job')
param resourcesMemory string = '1Gi'

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: containerAppsEnvironmentName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: userAssignedIdentityName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: azureContainerRegistryName
}

resource usersInitContainerJob 'Microsoft.App/jobs@2025-01-01' = {
  name: '${projectName}-${targetEnvironment}-users-init'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'users-init-container'
          image: initContainerImage
          resources: {
            cpu: json(resourcesCpu)
            memory: resourcesMemory
          }
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: targetEnvironment == 'prd' ? 'Production' : 'Staging'
            }
            {
              name: 'Users__CosmosDb__AccountEndpoint'
              value: cosmosDbEndpoint
            }
            {
              name: 'Users__CosmosDb__DatabaseName'
              value: cosmosDbDatabaseName
            }
            {
              name: 'Seeder__TenantsSeed'
              value: '${tenantsSeed}'
            }
            {
              name: 'Seeder__UsersSeed'
              value: '${usersSeed}'
            }
            {
              name: 'Seeder__SeedDataFilePath'
              value: '/app/seed-data'
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'UsersInitContainer'
            }
          ]
        }
      ]
    }
  }
  tags: {
    project: projectName
    environment: targetEnvironment
  }
}

output jobName string = usersInitContainerJob.name
