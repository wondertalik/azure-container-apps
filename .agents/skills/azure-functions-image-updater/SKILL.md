---
name: azure-functions-image-updater
description: Bump the Azure Functions docker base image version across all Azure Functions Dockerfiles in one operation. Use when told to update azure functions to a new version, upgrade the azure-functions docker image, or bump the functions host version. Requires the new version string (e.g. 4.XXXX.XXX-X). The dotnet-isolated suffix (-dotnet-isolated8.0) is preserved automatically. Triggers on 'update azure functions', 'bump functions image', 'upgrade azure-functions docker image', or 'update functions host version'.
---

# Skill: azure-functions-image-updater

Use this skill when a maintainer explicitly requests an Azure Functions docker base image version bump.

## Invocation cues

- "Update azure functions to 4.X.Y-Z"
- "Bump the azure-functions image to version X"
- "Upgrade functions host to 4.X.Y-Z"
- "Update azure functions docker image version"
- Diff or description mentions `azure-functions/dotnet-isolated:4.XXXX` changes

## Inputs

| Input              | Required      | Example        | Notes                           |
|--------------------|---------------|----------------|---------------------------------|
| New AzFunc version | Yes           | `4.XXXX.XXX-X` | Format: `4.XXXX.XXX-X`          |
| Old AzFunc version | Auto-detected | `<current-azfunc>` | Read from any AzFunc Dockerfile |

## Azure Functions Dockerfiles

The following projects use `mcr.microsoft.com/azure-functions/dotnet-isolated` as their base image:

- `src/FunctionApp1/Dockerfile`

Regular web/API/console Dockerfiles do **not** use this image and are not affected.

## Procedure

### Step 1 – Confirm the new version is provided

Verify the user has supplied the new Azure Functions version string (e.g. `4.XXXX.XXX-X`). If missing, ask for it before
continuing.

### Step 2 – Detect the current version

```bash
# Show the current azure-functions base image version
grep "azure-functions/dotnet-isolated" src/FunctionApp1/Dockerfile | head -1
```

Expected format: `mcr.microsoft.com/azure-functions/dotnet-isolated:4.XXXX.XXX-X-dotnet-isolated8.0`

### Step 3 – Bulk-replace the version across all Azure Functions Dockerfiles

The dotnet suffix (`-dotnet-isolated8.0`) is always preserved — only the version prefix changes.

```bash
OLD_AZFUNC="<current-azfunc>"     # replace with detected value
NEW_AZFUNC="<requested-azfunc>"   # replace with supplied value

find src -name "Dockerfile" | xargs grep -l "azure-functions/dotnet-isolated" | \
  xargs sed -i '' \
  "s|azure-functions/dotnet-isolated:${OLD_AZFUNC}-dotnet-isolated|azure-functions/dotnet-isolated:${NEW_AZFUNC}-dotnet-isolated|g"
```

> On Linux (CI/Docker) omit the `''` after `-i`.

### Step 4 – Verify no old version strings remain

```bash
echo "=== Remaining old AzFunc version references ==="
grep -rn "azure-functions/dotnet-isolated:${OLD_AZFUNC}" src \
  --include="Dockerfile" 2>/dev/null
```

Any remaining hits must be investigated and updated before finalising.

### Step 5 – Confirm all affected Dockerfiles were updated

```bash
echo "=== Updated AzFunc base images ==="
grep -rn "azure-functions/dotnet-isolated:" src \
  --include="Dockerfile" 2>/dev/null
```

All entries should show the new version.

## Output contract

- All Azure Functions `src/*/Dockerfile` files updated — `azure-functions/dotnet-isolated:OLD-dotnet-isolated8.0` replaced with
  `azure-functions/dotnet-isolated:NEW-dotnet-isolated8.0`
- Non-Azure-Functions Dockerfiles are untouched
- Zero old version strings remaining in any Dockerfile
- The `-dotnet-isolated8.0` suffix is unchanged in all files

## Stop and ask conditions

- New version string is not provided → ask before proceeding.
- Detected old version does not appear in any Dockerfile → confirm with user; the version may already be up to date.
- After replacement, some Dockerfiles still contain the old version → report which files and ask how to proceed.
- User asks to update only a subset of Azure Functions projects → explain that all must be kept in sync; update all or none.
