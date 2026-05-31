# Azure Functions for Containers - Infrastructure

This guide provides instructions on how to execute Bicep configuration using the Azure CLI.

## Prerequisites

- Azure CLI installed. If not, follow the [installation guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli).
- Bicep CLI installed. If not, follow the [installation guide](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install).
- A pre-existing resource group where resources will be deployed.

## Setup

1. Copy `main.bicepparam.example` to `main.bicepparam` and fill in your values:
   ```sh
   cp main.bicepparam.example main.bicepparam
   ```
   > Note: `main.bicepparam` is git-ignored and contains your actual deployment values.

## Steps to Execute Bicep Configuration

1. **Login to Azure:**
    ```sh
    az login
    ```

2. **Set the subscription (if necessary):**
    ```sh
    az account set --subscription <your-subscription-id>
    ```

3. **Create the resource group (if it doesn't exist):**
    ```sh
    az group create --name <your-resource-group> --location <location>
    ```

4. **Preview the changes (optional but recommended):**
    
    ```sh
    az deployment group what-if --resource-group <your-resource-group> --template-file ./main.bicep --parameters ./main.bicepparam
    ```

5. **Deploy the Bicep file:**
    ```sh
    az deployment group create --resource-group <your-resource-group> --template-file ./main.bicep --parameters ./main.bicepparam
    ```

## Example

```sh
az login
az account set --subscription 12345678-1234-1234-1234-123456789abc

# Create RG if needed
az group create --name rg-01-westeurope-learn-dev --location westeurope

# Preview
az deployment group what-if --resource-group rg-01-westeurope-learn-dev \
 --template-file ./main.bicep \
 --parameters ./main.bicepparam

# Deploy
az deployment group create --resource-group rg-01-westeurope-learn-dev \
 --template-file ./main.bicep \
 --parameters ./main.bicepparam
```

## Additional Resources

- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
