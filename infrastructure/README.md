# Azure Functions for Containers - Infrastructure

This guide provides instructions on how to execute Bicep configuration using the Azure CLI.

## Prerequisites

- Azure CLI installed. If not, follow the [installation guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli).
- Bicep CLI installed. If not, follow the [installation guide](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install).

## Steps to Execute Bicep Configuration

1. **Login to Azure:**
    ```sh
    az login
    ```

2. **Set the subscription (if necessary):**
    ```sh
    az account set --subscription <your-subscription-id>
    ```
    
3. **Preview the changes (optional but recommended):**
    
    ```sh
    az deployment sub what-if --resource-group <your-resource-group> --template-file <path-to-your-bicep-file> --parameters paramName=paramValue
    ```

    This command shows the changes that will be made by the deployment without actually applying them.

4. **Deploy the Bicep file:**
    ```sh
    az deployment sub create --resource-group <your-resource-group> --template-file <path-to-your-bicep-file> --parameters paramName=paramValue
    ```

    Replace `<your-resource-group>` with the name of your resource group and `<path-to-your-bicep-file>` with the path to your Bicep file.

## Example

```sh
az login
az account set --subscription 12345678-1234-1234-1234-123456789abc

targetenv='dev'
subscriptionId='12345678-1234-1234-1234-123456789abc'
location='westeurope'
projectName='learn'

az deployment sub what-if --location $location \
 --template-file ./main.bicep \
 --parameters \
   subscriptionId=$subscriptionId \
   applicationResourceGroupName="rg-01-${location}-${projectName}-${targetenv}" \
   location=$location \
   projectName=$projectName \
   targetEnvironment=$targetenv \
   azureContainerRegistryName='myexampleacrtst' \
   azureContainerRegistryResourceGroupName='rg-my-example-acr' \
   enableHttpApiContainerAppImage=true \
   httpApiContainerAppName='my-httpapi' \
   httpApiContainerAppImage='myexampleacrtst.azurecr.io/my-httpapi-tst:1.0.4-release' \
   httpApiContainerAppScaleMinReplicas=0 \
   httpApiContainerAppScaleMaxReplicas=1 \
   httpApiContainerAppResourcesCpu=0.5 \
   httpApiContainerAppResourcesMemory=1Gi \
   enableFunctionApp1Image=true \
   functionApp1Image='myexampleacrtst.azurecr.io/my-func-tst:2.0.10-release' \
   functionApp1MinimumElasticInstanceCount=0 \
   functionApp1ScaleLimit=1 \
   functionApp1ResourcesCpu=0.5 \
   functionApp1ResourcesMemory=1Gi

az deployment sub create --location $location \
 --template-file ./main.bicep \
 --parameters \
   subscriptionId=$subscriptionId \
   applicationResourceGroupName="rg-01-${location}-${projectName}-${targetenv}" \
   location=$location \
   projectName=$projectName \
   targetEnvironment=$targetenv \
   azureContainerRegistryName='myexampleacrtst' \
   azureContainerRegistryResourceGroupName='rg-my-example-acr' \
   enableHttpApiContainerAppImage=true \
   httpApiContainerAppName='my-httpapi' \
   httpApiContainerAppImage='myexampleacrtst.azurecr.io/my-httpapi-tst:1.0.4-release' \
   httpApiContainerAppScaleMinReplicas=0 \
   httpApiContainerAppScaleMaxReplicas=1 \
   httpApiContainerAppResourcesCpu=0.5 \
   httpApiContainerAppResourcesMemory=1Gi \
   enableFunctionApp1Image=true \
   functionApp1Image='myexampleacrtst.azurecr.io/my-func-tst:2.0.10-release' \
   functionApp1MinimumElasticInstanceCount=0 \
   functionApp1ScaleLimit=1 \
   functionApp1ResourcesCpu=0.5 \
   functionApp1ResourcesMemory=1Gi
```

## Additional Resources

- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
