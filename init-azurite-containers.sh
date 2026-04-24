#!/bin/bash

set -e

CONTAINERS=(
  "test-blob"
)

echo "Creating containers if they do not exist..."

for container in "${CONTAINERS[@]}"; do
  echo "Ensuring container: $container"
  az storage container create \
    --name "$container" \
    --connection-string "$AZURITE_CONNECTION_STRING"
done

echo "All containers ensured."
