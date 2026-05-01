# C# Code Formatting Setup

This repository includes comprehensive code formatting configuration for C# projects using EditorConfig and ReSharper Command Line Tools.

## Overview

The formatting setup includes:
- **Comprehensive .editorconfig** with C# best practices and 120-character line length
- **ReSharper Command Line Tools integration** for enterprise-grade formatting
- **Enhanced formatting scripts** with quoted arguments, project folder targeting, and file-specific formatting
- **Customizable cleanup profiles** with default "Built-in: Reformat & Apply Syntax Style"
- **Cross-platform scripts** for selective project formatting (PowerShell and Bash)
- **Build artifact cleanup scripts** for removing bin/obj directories
- **IDE integration** for Rider, Visual Studio, and VSCode

## Files Structure

```
backend/
├── .editorconfig                      # Comprehensive formatting rules with 120-character line length
├── Directory.Build.props              # MSBuild integration for ReSharper CLI formatting
├── clean.ps1                          # PowerShell script to clean build artifacts
├── clean.sh                           # Bash script to clean build artifacts
└── scripts/
    ├── format-csharp.ps1             # Enhanced PowerShell script with ReSharper CLI
    └── format-csharp.sh              # Enhanced Bash script with ReSharper CLI
```

## EditorConfig Rules

The `.editorconfig` file includes comprehensive rules for:

### General Rules
- **Indentation**: 4 spaces for C# code, 2 spaces for project files
- **Line length**: 120 characters maximum (matches Rider's "Hard wrap at: 120 symbols")
- **Line endings**: CRLF for consistency
- **Trailing whitespace**: Removed automatically
- **Final newline**: Added automatically

### C# Specific Rules
- **var usage**: Explicit types preferred, var only when type is apparent
- **this. qualification**: Avoided (not required)
- **Expression-bodied members**: Used for properties and accessors
- **Pattern matching**: Preferred over is/as checks
- **File-scoped namespaces**: Encouraged for .NET 6+
- **Using directives**: Outside namespace, sorted with System first
- **Braces**: Required for all control structures
- **Null-checking**: Modern patterns encouraged

### Code Analysis
- Configured severity levels for common analyzers
- Suppressed noisy rules while keeping important ones
- Emphasis on modern C# patterns and practices
[]
## Prerequisites

Before using the formatting scripts, you need to install ReSharper Command Line Tools:

```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools --version 2026.1.0
```

Verify the installation:
```bash
dotnet tool list -g JetBrains.ReSharper.GlobalTools
```

This provides the `jb` command used by the formatting scripts for comprehensive code cleanup and formatting.

## Usage

### Option 1: Manual Formatting with Scripts

#### Windows (PowerShell)
```powershell
# Format all C# projects
.\scripts\format-csharp.ps1

# Format specific projects
.\scripts\format-csharp.ps1 -Projects "Users.Management,OptiLeads.Services"

# Format specific files
.\scripts\format-csharp.ps1 -Files "file1.cs,file2.cs"

# Format with verbose output
.\scripts\format-csharp.ps1 -Projects "Users.Management" -Verbose

# Use custom cleanup profile
.\scripts\format-csharp.ps1 -Profile "Built-in: Full Cleanup"

# Include generated files
.\scripts\format-csharp.ps1 -IncludeGenerated
```

#### Unix/Linux/macOS (Bash)
```bash
# Format all C# projects
./scripts/format-csharp.sh

# Format specific projects
./scripts/format-csharp.sh -p "Users.Management,OptiLeads.Services"

# Format specific files
./scripts/format-csharp.sh -f "file1.cs,file2.cs"

# Format with verbose output
./scripts/format-csharp.sh -p "Users.Management" -v

# Use custom cleanup profile
./scripts/format-csharp.sh --profile "Built-in: Full Cleanup"

# Include generated files
./scripts/format-csharp.sh -g
```

### Option 2: MSBuild Integration

#### Enable formatting during build for all projects
```bash
dotnet build -p:FormatOnBuild=true
```

#### Enable formatting for specific project
```bash
dotnet build src/Users.Management/Users.Management.csproj -p:FormatOnBuild=true
```

#### Verify formatting in CI/CD
```bash
# Run formatting to ensure code is properly formatted
./scripts/format-csharp.sh    # Bash
.\scripts\format-csharp.ps1    # PowerShell
```

### Option 3: Direct ReSharper CLI usage

#### Format entire solution
```bash
jb cleanupcode CarOpticom-backend.sln --profile="Built-in: Reformat & Apply Syntax Style"
```

#### Format specific project folder
```bash
jb cleanupcode ./src/Users.Management --profile="Built-in: Reformat & Apply Syntax Style"
```

#### Exclude generated files
```bash
jb cleanupcode CarOpticom-backend.sln --profile="Built-in: Reformat & Apply Syntax Style" --exclude="**/*.Designer.cs;**/*.g.cs;**/*.g.i.cs;**/bin/**;**/obj/**"
```

## IDE Integration

### JetBrains Rider
1. EditorConfig rules are automatically detected and applied
2. Code cleanup profiles can be configured to match EditorConfig rules
3. Enable "Format code on save" in Settings → Editor → General → Auto Import

### Visual Studio
1. EditorConfig rules are automatically applied
2. Use Format Document (Ctrl+K, Ctrl+D) or Format Selection (Ctrl+K, Ctrl+F)
3. Configure automatic formatting in Tools → Options → Text Editor → C# → Code Style → Formatting

### Visual Studio Code
1. Install the C# extension
2. EditorConfig rules are automatically applied
3. Configure format on save in settings:
   ```json
   {
     "editor.formatOnSave": true,
     "editor.codeActionsOnSave": {
       "source.fixAll": true
     }
   }
   ```

## Formatting Tool Differences

The solution uses ReSharper Command Line Tools for automated formatting, which provides better alignment with Rider's formatting capabilities:

### ReSharper CLI vs IDE Formatting

- **ReSharper CLI (`jb cleanupcode`)**: Provides comprehensive code cleanup including formatting, using the same engine as Rider IDE
- **Rider IDE**: Uses the same ReSharper engine with sophisticated parameter alignment and intelligent line wrapping
- **Visual Studio**: Has its own formatting engine that may produce slightly different results
- **VSCode**: Uses OmniSharp which has basic formatting capabilities

### Consistency Recommendations

For the best team consistency:
1. **Primary tool**: Use the ReSharper CLI scripts for automated formatting
2. **IDE Configuration**: Configure your IDE to match the 120-character line length:
   - **Rider**: Set "Hard wrap at: 120 symbols" in Settings → Editor → Code Style → C# → Wrapping and Braces
   - **Visual Studio**: Set "Wrap at column: 120" in Tools → Options → Text Editor → C# → Code Style → Formatting → Wrapping
   - **VSCode**: EditorConfig automatically applies the 120-character limit

### EditorConfig Integration

The `.editorconfig` file ensures consistent basic formatting across all environments, while ReSharper CLI provides advanced code cleanup that matches Rider's capabilities.

## Project Selection Examples

The scripts support flexible project selection:

- **By exact name**: `"Users.Management"`
- **By partial name**: `"Users"` (matches all projects containing "Users")
- **By multiple projects**: `"Users.Management,OptiLeads.Services,Notification.Core"`
- **By project file name**: `"Users.Management.csproj"`

## Configuration Options

### MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EnableCodeFormatting` | `true` | Enable/disable formatting features |
| `FormatOnBuild` | `false` | Format code automatically during build |
| `VerifyCodeFormat` | `false` | Verify formatting without changes (for CI) |

### Script Parameters

#### PowerShell Script
| Parameter | Description |
|-----------|-------------|
| `-Projects` | Comma-separated list of projects to format |
| `-Files` | Comma-separated list of specific files to format |
| `-SolutionPath` | Path to solution file (auto-detected if not specified) |
| `-Profile` | ReSharper cleanup profile (default: "Built-in: Reformat & Apply Syntax Style") |
| `-IncludeGenerated` | Include generated files in formatting |
| `-Verbose` | Enable verbose output |

#### Bash Script
| Parameter | Description |
|-----------|-------------|
| `-p, --projects` | Comma-separated list of projects to format |
| `-f, --files` | Comma-separated list of specific files to format |
| `-s, --solution` | Path to solution file (auto-detected if not specified) |
| `--profile` | ReSharper cleanup profile (default: "Built-in: Reformat & Apply Syntax Style") |
| `-g, --include-generated` | Include generated files in formatting |
| `-v, --verbose` | Enable verbose output |

## Troubleshooting

### Common Issues

1. **"jb command not found"**
   - Install ReSharper Command Line Tools: `dotnet tool install -g JetBrains.ReSharper.GlobalTools`
   - Verify installation: `jb --version`

2. **"No solution file found"**
   - Specify the solution path explicitly: `-SolutionPath "path/to/solution.sln"`

3. **"Project not found"**
   - Check project name spelling
   - Use partial matching (e.g., "Users" instead of "Users.Management")
   - List all projects first without `-Projects` parameter

4. **Formatting changes not applied**
   - Ensure the project is a C# project (.csproj)
   - Check that files are not read-only
   - Verify EditorConfig syntax is correct

### Debugging

Enable verbose output to see detailed formatting information:
```bash
# PowerShell
.\scripts\format-csharp.ps1 -Verbose

# Bash
./scripts/format-csharp.sh -v
```

## Customization

### Modifying EditorConfig Rules

Edit `.editorconfig` to customize formatting rules. Key sections:

- **[*.cs]**: C# specific rules
- **[*.{cs,vb}]**: .NET general rules
- **Code Analysis Rules**: Diagnostic severity levels

### Adding Custom MSBuild Targets

Extend `Directory.Build.props` to add custom formatting behavior:

```xml
<Target Name="CustomFormatTarget" BeforeTargets="Build">
  <!-- Custom formatting logic -->
</Target>
```

## Best Practices

1. **Use consistent formatting**: Run formatting on entire codebase initially
2. **Enable IDE integration**: Configure your IDE to use EditorConfig rules
3. **Format before commits**: Use pre-commit hooks or format during CI/CD
4. **Team agreement**: Ensure all team members use the same configuration
5. **Regular updates**: Keep EditorConfig rules updated with team preferences

## IDE-Specific Formatting Differences

### Line Wrapping and Parameter Alignment

The EditorConfig includes `max_line_length = 120` to match Rider's "Hard wrap at: 120 symbols" setting. However, there are some differences between IDE formatters and `dotnet format`:

#### Rider/IntelliJ
- **Advanced line wrapping**: Rider provides sophisticated parameter alignment and method call wrapping
- **Intelligent breaking**: Automatically wraps long method calls, parameters, and expressions
- **Consistent alignment**: Parameters are aligned properly when wrapped to multiple lines

#### ReSharper CLI + EditorConfig
- **Enterprise-grade formatting**: Uses the same ReSharper engine as Rider IDE for sophisticated formatting
- **Advanced line wrapping**: Intelligent parameter alignment and method call wrapping at 120 characters
- **Comprehensive cleanup**: Goes beyond basic formatting to include code style improvements
- **Cross-platform consistency**: Identical results across all development environments

#### Recommendation
For the most consistent formatting across the team:
1. **Use ReSharper CLI scripts** for automated formatting in CI/CD and bulk operations
2. **Configure your IDE** to respect the EditorConfig settings for manual editing
3. **Set IDE line length to 120** to match the EditorConfig `max_line_length` setting
4. **Rider users**: Set "Hard wrap at: 120 symbols" in Settings → Editor → Code Style → C#
5. **Visual Studio users**: Set line length in Options → Text Editor → C# → Code Style → Formatting
6. **VSCode users**: Line length is automatically respected via the C# extension and EditorConfig

## Additional Utilities

### Cleaning Build Artifacts

The repository includes scripts to clean build artifacts (bin and obj directories):

#### Windows (PowerShell)
```powershell
.\clean.ps1
```

#### Unix/Linux/macOS (Bash)
```bash
./clean.sh
```

Both scripts will:
- Remove all `bin/` directories from `src/` and `tests/` folders
- Remove all `obj/` directories from `src/` and `tests/` folders
- Provide colored output showing which directories were removed

## GitHub Actions Integration

For continuous integration, you can use the formatting scripts in GitHub Actions:

```yaml
- name: Format Code
  run: |
    cd backend
    ./scripts/format-csharp.sh
```

## Support

For issues or questions about the formatting setup:
1. Check the troubleshooting section above
2. Review the EditorConfig documentation: https://editorconfig.org/
3. Consult ReSharper Command Line Tools documentation: https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html