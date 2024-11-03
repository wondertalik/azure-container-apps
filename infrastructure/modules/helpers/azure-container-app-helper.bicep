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
param containerAppImage string

@description('Target port of the container app')
param containerAppTargetPort int = 8080

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' existing = {
  name: containerAppsEnvironmentName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: userAssignedIdentityName
}

resource conatainerApp 'Microsoft.App/containerApps@2024-03-01' = {
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
        // Only add customDomains if optiLeadsEmailsApiManagedCertificateName is not empty
        //     customDomains: !empty(optiLeadsEmailsApiCertificateName)
        //       ? [
        //           {
        //             name: optiLeadsEmailsApiCustomUrl
        //             certificateId: optiLeadsEmailsApiCertificate.id
        //             bindingType: 'SniEnabled'
        //           }
        //         ]
        //       : null
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
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}