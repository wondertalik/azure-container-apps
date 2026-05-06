#!/bin/bash

#
# C# Code Formatter Script
# Formats C# projects using ReSharper Command Line Tools with comprehensive formatting rules.
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
PROJECTS=""
FILES=""
SOLUTION_PATH=""
INCLUDE_GENERATED=false
VERBOSE=false
PROFILE="Built-in: Reformat & Apply Syntax Style"

# Function to print colored output
print_color() {
    local color=$1
    local message=$2
    printf "${color}${message}${NC}\n"
}

# Function to print usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Format C# code using ReSharper Command Line Tools with comprehensive formatting rules."
    echo ""
    echo "Options:"
    echo "  -p, --projects PROJECTS     Comma-separated list of project names to format"
    echo "  -f, --files FILES           Comma-separated list of specific files to format"
    echo "  -s, --solution PATH         Path to the solution file"
    echo "      --profile PROFILE       ReSharper cleanup profile (default: 'Built-in: Reformat & Apply Syntax Style')"
    echo "  -g, --include-generated    Include generated files in formatting"
    echo "  -v, --verbose              Enable verbose output"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Format all C# projects"
    echo "  $0 -p \"Users.Management,OptiLeads\"   # Format specific projects"
    echo "  $0 -f \"file1.cs,file2.cs\"            # Format specific files"
    echo "  $0 -p \"Users.Management\" -v           # Format specific project with verbose output"
    echo "  $0 --profile \"Built-in: Full Cleanup\" # Use specific cleanup profile"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--projects)
            PROJECTS="$2"
            shift 2
            ;;
        -f|--files)
            FILES="$2"
            shift 2
            ;;
        -s|--solution)
            SOLUTION_PATH="$2"
            shift 2
            ;;
        --profile)
            PROFILE="$2"
            shift 2
            ;;
        -g|--include-generated)
            INCLUDE_GENERATED=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Function to find solution file
find_solution_file() {
    if [[ -n "$SOLUTION_PATH" && -f "$SOLUTION_PATH" ]]; then
        echo "$SOLUTION_PATH"
        return 0
    fi
    
    local solution_file=$(find . -name "*.sln" -type f | head -n 1)
    if [[ -n "$solution_file" ]]; then
        echo "$solution_file"
        return 0
    fi
    
    print_color $RED "ERROR: No solution file found. Please specify -s/--solution PATH parameter."
    exit 1
}

# Function to get C# projects from solution
get_csharp_projects() {
    local solution_file=$1
    local solution_dir=$(dirname "$solution_file")
    
    # Extract C# project paths from solution file
    grep -E 'Project.*\.csproj' "$solution_file" | while IFS= read -r line; do
        # Use sed to extract project name and path
        local project_name=$(echo "$line" | sed -n 's/.*= "\([^"]*\)".*/\1/p')
        local project_path=$(echo "$line" | sed -n 's/.*", "\([^"]*\.csproj\)".*/\1/p')
        
        if [[ -n "$project_name" && -n "$project_path" ]]; then
            # Convert Windows backslashes to forward slashes
            project_path=$(echo "$project_path" | sed 's|\\|/|g')
            local full_path="$solution_dir/$project_path"
            
            if [[ -f "$full_path" ]]; then
                echo "$project_name|$full_path|$project_path"
            fi
        fi
    done
}

# Function to format individual files
format_files() {
    local files_list=$1
    local include_generated=$2
    local is_verbose=$3
    local profile=$4
    
    IFS=',' read -ra file_array <<< "$files_list"
    local success_count=0
    local failure_count=0
    
    for file_path in "${file_array[@]}"; do
        file_path=$(echo "$file_path" | xargs) # trim whitespace
        
        if [[ ! -f "$file_path" ]]; then
            print_color $YELLOW "WARNING: File not found: $file_path"
            failure_count=$((failure_count + 1))
            continue
        fi
        
        local format_args=("cleanupcode" "$file_path" "--profile=$profile")
        
        if [[ "$include_generated" == "true" ]]; then
            format_args+=("--include=*")
        else
            format_args+=("--exclude=**/*.Designer.cs;**/*.g.cs;**/*.g.i.cs;**/*.cshtml")
        fi
        
        if [[ "$is_verbose" == "true" ]]; then
            format_args+=("--verbosity=INFO")
        else
            format_args+=("--verbosity=WARN")
        fi
        
        # Print the exact command that will be executed with quoted arguments
        local quoted_args=""
        for arg in "${format_args[@]}"; do
            if [[ "$arg" == cleanupcode ]]; then
                quoted_args="$arg"
            elif [[ "$arg" == --verbosity=* ]]; then
                quoted_args="$quoted_args $arg"
            elif [[ "$arg" == --profile=* ]]; then
                local option_value="${arg#--profile=}"
                quoted_args="$quoted_args --profile=\"$option_value\""
            elif [[ "$arg" == --exclude=* ]]; then
                local option_value="${arg#--exclude=}"
                quoted_args="$quoted_args --exclude=\"$option_value\""
            elif [[ "$arg" == --include=* ]]; then
                local option_value="${arg#--include=}"
                quoted_args="$quoted_args --include=\"$option_value\""
            else
                quoted_args="$quoted_args \"$arg\""
            fi
        done
        print_color $CYAN "Command: jb $quoted_args"
        
        print_color $GREEN "Formatting: $file_path"
        
        # Log files that would be formatted for debugging
        if [[ -f "$file_path" ]]; then
            print_color $NC "  -> File will be formatted: $file_path"
        fi
        
        if jb "${format_args[@]}" >/dev/null 2>&1; then
            print_color $GREEN "SUCCESS: $file_path - Formatted successfully"
            print_color $NC "  -> File formatted: $file_path"
            success_count=$((success_count + 1))
        else
            print_color $RED "FAILED: $file_path - Formatting failed"
            failure_count=$((failure_count + 1))
        fi
    done
    
    return $failure_count
}

# Function to format a project
format_project() {
    local project_name=$1
    local project_path=$2
    local include_generated=$3
    local is_verbose=$4
    local profile=$5
    
    # Use project directory instead of .csproj file
    local project_dir=$(dirname "$project_path")
    
    local format_args=("cleanupcode" "$project_dir" "--profile=$profile")
    
    if [[ "$include_generated" == "true" ]]; then
        format_args+=("--include=*")
    else
        format_args+=("--exclude=**/*.Designer.cs;**/*.g.cs;**/*.g.i.cs;**/*.cshtml;**/bin/**;**/obj/**")
    fi
    
    if [[ "$is_verbose" == "true" ]]; then
        format_args+=("--verbosity=INFO")
    else
        format_args+=("--verbosity=WARN")
    fi
    
    # Print the exact command that will be executed with quoted arguments
    local quoted_args=""
    for arg in "${format_args[@]}"; do
        if [[ "$arg" == cleanupcode ]]; then
            quoted_args="$arg"
        elif [[ "$arg" == --verbosity=* ]]; then
            quoted_args="$quoted_args $arg"
        elif [[ "$arg" == --profile=* ]]; then
            local option_value="${arg#--profile=}"
            quoted_args="$quoted_args --profile=\"$option_value\""
        elif [[ "$arg" == --exclude=* ]]; then
            local option_value="${arg#--exclude=}"
            quoted_args="$quoted_args --exclude=\"$option_value\""
        elif [[ "$arg" == --include=* ]]; then
            local option_value="${arg#--include=}"
            quoted_args="$quoted_args --include=\"$option_value\""
        else
            quoted_args="$quoted_args \"$arg\""
        fi
    done
    print_color $CYAN "Command: jb $quoted_args"
    
    print_color $GREEN "Formatting: $project_name"
    
    # Get list of C# files that will be formatted for debugging
    local cs_files=($(find "$project_dir" -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" ! -name "*.Designer.cs" ! -name "*.g.cs" ! -name "*.g.i.cs" 2>/dev/null || true))
    print_color $NC "  -> Found ${#cs_files[@]} C# files to format in $project_name"
    
    if jb "${format_args[@]}" >/dev/null 2>&1; then
        print_color $GREEN "SUCCESS: $project_name - Formatted successfully"
        if [[ ${#cs_files[@]} -gt 0 ]]; then
            print_color $NC "  -> Formatted ${#cs_files[@]} files in $project_name"
            if [[ ${#cs_files[@]} -le 10 ]]; then
                for file in "${cs_files[@]}"; do
                    local relative_path=${file#$(pwd)/}
                    print_color $NC "    - $relative_path"
                done
            else
                print_color $NC "    - (showing first 5 of ${#cs_files[@]} files)"
                for ((i=0; i<5; i++)); do
                    local relative_path=${cs_files[$i]#$(pwd)/}
                    print_color $NC "    - $relative_path"
                done
                print_color $NC "    - ... and $((${#cs_files[@]} - 5)) more files"
            fi
        fi
        return 0
    else
        print_color $RED "FAILED: $project_name - Formatting failed"
        return 1
    fi
}

# Main execution
main() {
    print_color $CYAN "C# Code Formatter"
    print_color $CYAN "==================="
    
    # Check if we're formatting individual files
    if [[ -n "$FILES" ]]; then
        print_color $BLUE "Formatting individual files..."
        print_color $BLUE "Using profile: $PROFILE"
        
        format_files "$FILES" "$INCLUDE_GENERATED" "$VERBOSE" "$PROFILE"
        failure_count=$?
        
        # Summary for file formatting
        echo ""
        print_color $CYAN "Summary:"
        print_color $CYAN "=========="
        
        if [[ $failure_count -eq 0 ]]; then
            echo ""
            print_color $GREEN "All files processed successfully!"
            exit 0
        else
            echo ""
            print_color $RED "Some files failed to process."
            exit 1
        fi
    fi
    
    # Find solution file
    solution_file=$(find_solution_file)
    print_color $BLUE "Using solution: $(basename "$solution_file")"
    print_color $BLUE "Using profile: $PROFILE"
    
    # Get all C# projects (portable alternative to mapfile)
    all_projects=()
    while IFS= read -r line; do
        all_projects+=("$line")
    done < <(get_csharp_projects "$solution_file")
    
    if [[ ${#all_projects[@]} -eq 0 ]]; then
        print_color $YELLOW "WARNING: No C# projects found in the solution."
        exit 0
    fi
    
    print_color $BLUE "Found ${#all_projects[@]} C# projects"
    
    # Determine which projects to format
    declare -a projects_to_format
    
    if [[ -n "$PROJECTS" ]]; then
        IFS=',' read -ra requested_projects <<< "$PROJECTS"
        for requested in "${requested_projects[@]}"; do
            requested=$(echo "$requested" | xargs) # trim whitespace
            found=false
            matched_project=""
            
            # First pass: Look for exact matches (project name or basename)
            for project_info in "${all_projects[@]}"; do
                IFS='|' read -r project_name project_path project_relative_path <<< "$project_info"
                
                if [[ "$project_name" == "$requested" ]] || \
                   [[ "$(basename "$project_path" .csproj)" == "$requested" ]]; then
                    matched_project="$project_info"
                    found=true
                    break
                fi
            done
            
            # Second pass: If no exact match found, look for partial matches in path
            if [[ "$found" == "false" ]]; then
                for project_info in "${all_projects[@]}"; do
                    IFS='|' read -r project_name project_path project_relative_path <<< "$project_info"
                    
                    if [[ "$project_relative_path" == *"$requested"* ]]; then
                        matched_project="$project_info"
                        found=true
                        break
                    fi
                done
            fi
            
            if [[ "$found" == "true" ]]; then
                projects_to_format+=("$matched_project")
            else
                print_color $YELLOW "WARNING: Project not found: $requested"
            fi
        done
    else
        projects_to_format=("${all_projects[@]}")
    fi
    
    if [[ ${#projects_to_format[@]} -eq 0 ]]; then
        print_color $RED "ERROR: No matching projects found to format."
        exit 1
    fi
    
    print_color $BLUE "Formatting ${#projects_to_format[@]} projects..."
    
    # Format projects
    success_count=0
    failure_count=0
    
    for project_info in "${projects_to_format[@]}"; do
        IFS='|' read -r project_name project_path project_relative_path <<< "$project_info"
        
        if format_project "$project_name" "$project_path" "$INCLUDE_GENERATED" "$VERBOSE" "$PROFILE"; then
            success_count=$((success_count + 1))
        else
            failure_count=$((failure_count + 1))
        fi
    done
    
    # Summary
    echo ""
    print_color $CYAN "Summary:"
    print_color $CYAN "=========="
    print_color $GREEN "Successful: $success_count"
    
    if [[ $failure_count -gt 0 ]]; then
        print_color $RED "Failed: $failure_count"
    fi
    
    if [[ $failure_count -eq 0 ]]; then
        echo ""
        print_color $GREEN "All projects processed successfully!"
        exit 0
    else
        echo ""
        print_color $RED "Some projects failed to process."
        exit 1
    fi
}

# Check if ReSharper CLI tools are available
if ! jb cleanupcode --help &> /dev/null; then
    print_color $RED "ERROR: ReSharper Command Line Tools (jb) are not installed."
    print_color $YELLOW "TIP: Install them with: dotnet tool install -g JetBrains.ReSharper.GlobalTools"
    exit 1
fi

# Run main function
main "$@"