#!/bin/bash
set -e

echo "Starting Users.InitContainer..."
exec dotnet Users.InitContainer.dll "$@"
