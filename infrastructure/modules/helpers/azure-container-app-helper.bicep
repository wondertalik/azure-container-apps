@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Azure container app alias')
param alias string

@description('Target environment')
param targetEnvironment string

@description('Container apps environment')
param containerAppsEnvironmentName string

@description('Name of the user assigned identity resource')
param userAssignedIdentityName string

@description('Name of azure container registry')
param azureContainerRegistryName string

@description('Full image name of the container app')
param containerAppName string

@description('Full image name of the container app')
param containerAppImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('Target port of the container app')
param containerAppTargetPort int = 8080

@description('CPU resources')
param resourcesCpu string

@description('Memory resources')
param resourcesMemory string

@description('Scale min replicas')
param scaleMinReplicas int = 0

@description('Scale max replicas')
param scaleMaxReplicas int = 3

@description('Enable health probes for the container app')
param enableHealthProbes bool = false

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: containerAppsEnvironmentName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: userAssignedIdentityName
}

resource conatainerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'ca-${containerAppName}-${projectName}-${targetEnvironment}' // 32 symbols 
  location: location
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
    alias: alias
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
      ingress: {
        targetPort: containerAppTargetPort
        external: true
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: '${azureContainerRegistryName}.azurecr.io'
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: containerAppImage
          resources: {
            cpu: json(resourcesCpu)
            memory: resourcesMemory
          }
          probes: enableHealthProbes
            ? [
                {
                  type: 'Liveness'
                  failureThreshold: 3
                  periodSeconds: 10
                  initialDelaySeconds: 7
                  httpGet: {
                    path: '/healthz'
                    port: 8080
                    scheme: 'HTTP'
                    httpHeaders: [
                      {
                        name: 'Custom-Header'
                        value: 'liveness probe'
                      }
                    ]
                  }
                }
                {
                  type: 'Readiness'
                  failureThreshold: 3
                  initialDelaySeconds: 10
                  httpGet: {
                    path: '/healthz'
                    port: 8080
                    scheme: 'HTTP'
                    httpHeaders: [
                      {
                        name: 'Custom-Header'
                        value: 'readiness probe'
                      }
                    ]
                  }
                }
                {
                  type: 'Startup'
                  initialDelaySeconds: 3
                  periodSeconds: 3
                  httpGet: {
                    path: '/healthz'
                    port: 8080
                    scheme: 'HTTP'
                    httpHeaders: [
                      {
                        name: 'Custom-Header'
                        value: 'startup probe'
                      }
                    ]
                  }
                }
              ]
            : []
        }
      ]
      scale: {
        minReplicas: scaleMinReplicas
        maxReplicas: scaleMaxReplicas
      }
    }
  }
}
