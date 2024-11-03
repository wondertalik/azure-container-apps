@description('Name of the project related resources')
param projectName string

@description('Location of newly created resources')
param location string

@description('Target environment')
param targetEnvironment string

@description('Prefix of user telemetry resources')
param namePrefix string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${namePrefix}-${location}-${projectName}-${targetEnvironment}'
  location: location
  properties: {
    retentionInDays: 90
    sku: {
      name: 'PerGB2018'
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {  
  name: 'appi-${namePrefix}-${location}-${projectName}-${targetEnvironment}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    SamplingPercentage: 5
    RetentionInDays: 90
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}
output applicationInsightsName string = applicationInsights.name
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
