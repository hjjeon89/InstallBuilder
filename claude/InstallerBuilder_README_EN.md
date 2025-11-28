# InstallerBuilder Project Documentation

## Project Overview

InstallerBuilder is a GUI application that automatically generates Windows installer files (EXE) for multiple C# projects.

### Key Features

- **Multi-Project Support**: Add as many projects as needed and generate installers for each
- **.NET Framework & .NET Core/5+ Support**: Automatic detection and appropriate build tool selection
- **Inno Setup Integration**: Professional Windows installer creation
- **DevExpress License Support**: Handles projects with licenses.licx files
- **Desktop Shortcut Creation**: Automatically creates shortcuts during installation

## Technology Stack

- **Development Environment**: .NET 9.0 Windows Forms
- **Supported Project Types**:
  - .NET Framework 2.0 ~ 4.8.x
  - .NET Core 3.0+
  - .NET 5.0+
- **Build Tools**:
  - MSBuild (Visual Studio 2019/2022)
  - dotnet CLI
- **Installer Generation**: Inno Setup 5/6

## System Requirements

### Required
- Windows 10 or later
- .NET 9.0 Runtime (for execution)
- Visual Studio 2019 or later (for building .NET Framework projects)

### Optional
- Inno Setup 5 or 6 (for installer generation)

## Architecture and Design

### Project Type Detection Logic

```csharp
private bool IsNetFrameworkProject(string projectContent)
{
    // 1. Check for TargetFrameworkVersion tag (legacy projects)
    if (projectContent.Contains("<TargetFrameworkVersion>"))
        return true;

    // 2. Check for Microsoft.NET.Sdk.WindowsDesktop - .NET Core 3.0+ WinForms
    if (projectContent.Contains("Microsoft.NET.Sdk.WindowsDesktop"))
        return false;

    // 3. Check for net4.x, net35, net20
    if (Regex.IsMatch(projectContent, @"<TargetFramework>net(4\d{1,2}|3[05]|20)</TargetFramework>"))
        return true;

    // 4. Check for legacy project format
    if (projectContent.Contains("http://schemas.microsoft.com/developer/msbuild/2003"))
        return true;

    return false;
}
```

### Build Strategy

#### MSBuild Usage Conditions
1. .NET Framework projects
2. Projects containing licenses.licx file (all .NET versions)

**Reason**: LC (License Compiler) task is not supported in .NET Core's `dotnet build` and only works with .NET Framework MSBuild

#### MSBuild Command
```bash
MSBuild.exe "<ProjectPath>" /restore /p:Configuration=Release /p:Platform="<Platform>" /v:normal /t:Rebuild
```

**Key Options**:
- `/restore`: Automatic NuGet package restoration
- `/p:Configuration=Release`: Release build
- `/p:Platform="<Platform>"`: Platform specification (Any CPU, x86, x64)
- `/v:normal`: Verbose logging
- `/t:Rebuild`: Full rebuild

#### dotnet build Usage
.NET Core/5+ projects (without licenses.licx)
```bash
dotnet build "<ProjectPath>" -c Release
```

### Platform Detection Logic

```csharp
private string DetectPlatform(string projectContent)
{
    // 1. Check Release Configuration's PlatformTarget
    var releaseMatch = Regex.Match(
        projectContent,
        @"<PropertyGroup[^>]*Condition[^>]*Release[^>]*>[\s\S]*?<PlatformTarget>([^<]+)</PlatformTarget>"
    );

    if (releaseMatch.Success)
        return releaseMatch.Groups[1].Value == "AnyCPU" ? "Any CPU" : releaseMatch.Groups[1].Value;

    // 2. Check general PlatformTarget
    // 3. Default: "Any CPU"
}
```

**Important**: Platform names with spaces like "Any CPU" must be quoted when passed as MSBuild arguments

### Inno Setup Script Generation

```inno
[Setup]
AppId={{GUID}}
AppName={ProjectName}
AppVersion=1.0
AppPublisher=GreenPower
DefaultDirName={autopf}\{ProjectName}
OutputDir=<OutputPath>
OutputBaseFilename={ProjectName}_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "<BuildOutput>/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{ProjectName}"; Filename: "{app}\{ProjectName}.exe"
Name: "{autodesktop}\{ProjectName}"; Filename: "{app}\{ProjectName}.exe"

[Run]
Filename: "{app}\{ProjectName}.exe"; Description: "Launch Program"; Flags: nowait postinstall skipifsilent
```

## Major Challenges and Solutions

### 1. Encoding Issue (CP949)

**Problem**: .NET Core doesn't include CP949 encoding by default
```
No data is available for encoding 949
```

**Solution**:
```csharp
// Program.cs
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// InstallerBuilder.csproj
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
```

### 2. MSBuild Path Issues

**Problem**: Parsing errors when executing through cmd.exe with paths containing spaces

**Solution**: Execute Process directly (without cmd.exe)
```csharp
private async Task RunProcessDirectlyAsync(string exePath, string arguments)
{
    process.StartInfo.FileName = exePath;
    process.StartInfo.Arguments = arguments;
    process.StartInfo.UseShellExecute = false;
    // No cmd.exe usage
}
```

### 3. Inno Setup Path Issues

**Problem**: Backslash escaping issues
```
D:\\__work\\__src -> Inno Setup parsing error
```

**Solution**: Convert to forward slashes
```csharp
string outputDirForScript = outputDir.Replace("\\", "/");
// D:/__work/__src
```

### 4. licenses.licx LC Task Error

**Problem**: When using `dotnet build` on .NET Core/5+
```
error MSB4803: The "LC" task is not supported in the .NET Core version of MSBuild.
```

**Solution**: Use MSBuild for all projects with licenses.licx
```csharp
bool needsMSBuild = isNetFramework || hasLicensesFile;
```

### 5. MSB1008 Error - Project Specification Error

**Problem**: MSBuild parsing error when Platform value contains spaces
```
/p:Platform=Any CPU  // Wrong - "CPU" recognized as separate argument
```

**Solution**: Quote values with spaces
```csharp
if (platform.Contains(" "))
    platformArg = $" /p:Platform=\"{platform}\"";
else
    platformArg = $" /p:Platform={platform}";
```

## Project Structure

```
ModuleCycler_2025/
├── InstallerBuilder/
│   ├── Form1.cs              # Main logic
│   ├── Form1.Designer.cs     # UI design
│   ├── Program.cs            # Entry point, encoding setup
│   └── InstallerBuilder.csproj
├── install/                  # Installer output directory
└── installer_cmd.txt         # Requirements document
```

## Usage Guide

### 1. Launch InstallerBuilder
```bash
dotnet run --project InstallerBuilder/InstallerBuilder.csproj
# or
InstallerBuilder.exe
```

### 2. Add Projects
1. Enter .csproj file path in "Project Path" textbox
2. Use "Browse..." button for file selection
3. Click "Add Project" button
4. Repeat as needed

### 3. Set Output Path
- Default: `D:\Installer`
- Change using "Browse..." button

### 4. Generate Installers
- Click "Generate Installer" button
- Monitor progress in log window

## Log Output Format

```
Building project: CANDBEditor.csproj
.NET Framework project detected - Using MSBuild.exe
licenses.licx file detected - MSBuild.exe required (LC task)
MSBuild path: C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
MSBuild build started (platform: Any CPU)
MSBuild command: "C:\...\MSBuild.exe" "D:\...\CANDBEditor.csproj" /restore /p:Configuration=Release /p:Platform="Any CPU" /v:normal /t:Rebuild
[EXEC] C:\...\MSBuild.exe ...
[OUT] Build output line
[ERR] Error output line
[EXIT CODE] 0
Build output path: D:\...\bin\Release\net8.0-windows
```

## Supported Projects

### Current Projects (installer_cmd.txt)
1. **GreenPowerCycler** - Main battery cycler program
2. **ScheduleEditor** - Schedule editor (.NET Framework + DevExpress)
3. **CyclerDataAnalyzer** - Data analysis tool
4. **CANDBEditor** - CAN database editor (.NET 8.0 + DevExpress)

## Installer Output

Generated files:
```
D:\Installer\
├── GreenPowerCycler_Setup.exe
├── ScheduleEditor_Setup.exe
├── CyclerDataAnalyzer_Setup.exe
└── CANDBEditor_Setup.exe
```

Each installer:
- Creates desktop shortcut
- Creates Start menu entry
- Registers in Add/Remove Programs
- Provides Korean installation interface

## Troubleshooting

### MSBuild Not Found
**Symptom**: "MSBuild not found" message

**Solution**:
1. Install Visual Studio 2019 or 2022
2. Install "Desktop development with .NET" workload
3. Verify MSBuild path:
   - VS 2022: `C:\Program Files\Microsoft Visual Studio\2022\{Edition}\MSBuild\Current\Bin\MSBuild.exe`
   - Edition: Community, Professional, Enterprise

### Inno Setup Not Found
**Symptom**: "Inno Setup unavailable" message

**Solution**:
1. [Download Inno Setup](https://jrsoftware.org/isdl.php)
2. Install to default path:
   - `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

### licenses.licx Related Errors
**Symptom**: MSB4803 error

**Cause**: Attempted build with dotnet build

**Automatic Resolution**: InstallerBuilder automatically uses MSBuild

## Future Improvements

1. **WiX Toolset Support**: MSI file generation (implemented but unused)
2. **Parallel Builds**: Simultaneous building of multiple projects
3. **Build Cache**: Avoid rebuilding unchanged projects
4. **Version Management**: Automatic version incrementing
5. **Signing Support**: Code signing integration

## License and Contribution

- **Project**: GreenPower Cycler Suite
- **Development**: Claude Code + User
- **Version**: 1.0
- **Date**: 2025

## References

- [MSBuild Command-Line Reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [.NET CLI Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [DevExpress License Compiler](https://docs.devexpress.com/GeneralInformation/2035/licensing)
