#!/bin/bash

# Install certificates if any are mounted
if [ -d "/certs" ] && [ "$(ls -A /certs 2>/dev/null)" ]; then
    echo "Installing certificates from /certs directory..."
    # Copy all .crt files to the trusted certificates directory
    find /certs -name "*.crt" -exec cp {} /usr/local/share/ca-certificates/ \;
    # Update the certificate store
    update-ca-certificates
    echo "Certificates installed successfully."
else
    echo "No certificates found in /certs directory. Skipping certificate installation."
fi

# Run the original application
exec dotnet ManualContainerAppJob.dll