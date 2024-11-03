@description('Location of newly created resources')
param location string

@maxLength(30)
@description('Name of resouce group')
param resourceGroupName string

targetScope = 'subscription'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

output resourceGroupName string = resourceGroup.name
