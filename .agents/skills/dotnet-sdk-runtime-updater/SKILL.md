---
name: dotnet-sdk-runtime-updater
description: Bump .NET SDK and ASP.NET Core runtime versions across all Dockerfiles and global.json in one atomic operation. Use when told to update dotnet sdk to a new version, bump .NET runtime, or upgrade aspnet image. Always requires BOTH sdk version (e.g. 8.0.4XX) AND aspnet version (e.g. 8.0.XX) — rejects if only one is supplied. Triggers on 'update dotnet sdk', 'bump .NET version', 'update aspnet runtime', or 'upgrade dotnet images'.
---

# Skill: dotnet-sdk-runtime-updater

Use this skill when a maintainer explicitly requests a .NET SDK and/or ASP.NET Core runtime version bump.

## Coupling rule

**SDK and ASP.NET Core runtime versions MUST be updated together in a single operation.**

- If the user supplies only a new SDK version without an aspnet version → stop and ask for both.
- If the user supplies only a new aspnet version without an SDK version → stop and ask for both.
- Only proceed when both values are confirmed.

## Invocation cues

- "Update dotnet sdk to X.X.XXX"
- "Bump .NET SDK to X.X.XXX and aspnet to X.X.XX"
- "Upgrade aspnet runtime to X.X.XX"
- "Update .NET images in Dockerfiles"
- Diff or description mentions `sdk:8.0.XXX` or `aspnet:8.0.XX` changes

## Inputs

| Input              | Required      | Example   | Notes                       |
|--------------------|---------------|-----------|-----------------------------|
| New SDK version    | Yes           | `8.0.4XX` | Three-part patch: `8.0.XXX` |
| New aspnet version | Yes           | `8.0.XX`  | Two-part patch: `8.0.XX`    |
| Old SDK version    | Auto-detected | `<current-sdk>` | Read from `global.json` |
| Old aspnet version | Auto-detected | `<current-aspnet>` | Read from any Dockerfile |

## Affected files

| File                                 | What changes                        |
|--------------------------------------|-------------------------------------|
| `global.json`                        | SDK version                         |
| `src/FunctionApp1/Dockerfile`        | SDK build stage                     |
| `src/HttpApi/Dockerfile`             | SDK build stage + aspnet base stage |
| `src/Users.InitContainer/Dockerfile` | SDK build stage                     |

## Procedure

### Step 1 – Confirm both versions are provided

Verify the user has supplied both a new SDK version and a new aspnet version. If either is missing, stop and request both before
continuing.

### Step 2 – Detect current versions

```bash
# Current SDK version
grep '"version"' global.json

# Current aspnet version (any non-AzFunc Dockerfile)
grep 'aspnet:' src/HttpApi/Dockerfile | head -1
```

### Step 3 – Update global.json

```bash
OLD_SDK="<current-sdk>"       # replace with detected value
NEW_SDK="<requested-sdk>"     # replace with supplied value

sed -i '' "s/\"version\": \"${OLD_SDK}\"/\"version\": \"${NEW_SDK}\"/" global.json
```

> On Linux (CI/Docker) omit the `''` after `-i`.

### Step 4 – Update all Dockerfiles (SDK build stage)

All Dockerfiles use `mcr.microsoft.com/dotnet/sdk` in the build stage.

```bash
find src -name "Dockerfile" | xargs sed -i '' \
  "s|mcr.microsoft.com/dotnet/sdk:${OLD_SDK}|mcr.microsoft.com/dotnet/sdk:${NEW_SDK}|g"
```

### Step 5 – Update non-Azure-Functions Dockerfiles (aspnet base stage)

Only web/API/console Dockerfiles use `mcr.microsoft.com/dotnet/aspnet`. Azure Functions Dockerfiles use a different base image and
are not affected.

```bash
OLD_ASPNET="<current-aspnet>"     # replace with detected value
NEW_ASPNET="<requested-aspnet>"   # replace with supplied value

find src -name "Dockerfile" | xargs grep -l "aspnet:" | xargs sed -i '' \
  "s|mcr.microsoft.com/dotnet/aspnet:${OLD_ASPNET}|mcr.microsoft.com/dotnet/aspnet:${NEW_ASPNET}|g"
```

### Step 6 – Verify no old version strings remain

Check that no stale `.NET 8` SDK or aspnet patch versions remain in any of the affected files.

```bash
echo "=== Remaining SDK references (any 8.0.4XX != NEW_SDK) ==="
grep -rn "8\.0\.[0-9][0-9][0-9]" \
  global.json \
  src \
  --include="Dockerfile" \
  --include="*.json" \
  2>/dev/null | grep -v ".git/" | grep -v "${NEW_SDK}"

echo "=== Remaining aspnet references (any 8.0.XX != NEW_ASPNET) ==="
grep -rn "aspnet:8\.0\.[0-9][0-9]" \
  src \
  --include="Dockerfile" \
  2>/dev/null | grep -v ".git/" | grep -v "${NEW_ASPNET}"
```

Any remaining hits must be investigated and updated before finalising.

## Output contract

- `global.json` — sdk version updated
- All `src/*/Dockerfile` — `sdk:OLD` replaced with `sdk:NEW`
- Non-Azure-Functions `src/*/Dockerfile` — `aspnet:OLD` replaced with `aspnet:NEW`
- Zero stale `.NET 8` SDK or aspnet version strings remaining in any of the above files

## Stop and ask conditions

- Only one of SDK / aspnet version is provided → ask for both before proceeding.
- Detected old version does not match any Dockerfile or `global.json` → confirm with user before replacing.
- Any file listed above cannot be found → report missing file and ask how to proceed.
- User asks to update SDK only for a specific project → explain the coupling rule; all projects must be updated together.
