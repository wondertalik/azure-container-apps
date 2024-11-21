@description('Location of newly created resources')
param location string

@description('Name of the project related resources')
param projectName string

@description('Target environment')
param targetEnvironment string

@description('Name of the log analytics workspace resource')
param logAnalyticsWorkspaceName string

@description('Name of the application insights name resource')
param applicationInsightsName string

@description('Prefix of user assigned identity')
param namePrefix string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: 'vnet-01-${location}-${projectName}-${targetEnvironment}'
  location: location
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
  }
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'snet-01-${location}-${projectName}-${targetEnvironment}'
        properties: {
          addressPrefix: '10.0.0.0/24'
          delegations: [
            {
              name: 'Microsoft.App/environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
    ]
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' = {
  name: 'cae-${namePrefix}-${location}-${projectName}-${targetEnvironment}'
  location: location
  tags: {
    environment: targetEnvironment
    project: projectName
    location: location
  }
  properties: {
    peerAuthentication: {
      //check docs what is this
      mtls: {
        enabled: false
      }
    }
    peerTrafficConfiguration: {
      encryption: {
        enabled: false
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    daprAIInstrumentationKey: applicationInsights.properties.InstrumentationKey
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
        dynamicJsonColumns: false
      }
    }
    infrastructureResourceGroup: 'rg-01-infrastructure-cae-${namePrefix}-${location}-${projectName}-${targetEnvironment}'
    vnetConfiguration: {
      infrastructureSubnetId: vnet.properties.subnets[0].id
    }
    publicNetworkAccess: 'Enabled'
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: ['appInsights']
      }
      logsConfiguration: {
        destinations: ['appInsights']
      }
    }
    appInsightsConfiguration: {
      connectionString: applicationInsights.properties.InstrumentationKey
    }
  }
}

resource dotNetComponent 'Microsoft.App/managedEnvironments/dotNetComponents@2024-08-02-preview' = {
  name: 'aspire-dashboard'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'AspireDashboard'
  }
}

output containerAppsEnvironmentName string = containerAppsEnvironment.name
