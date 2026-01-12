using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace InstallerBuilder;

public partial class Form1 : Form
{
    private List<string> dllFiles = new List<string>();
    private List<string> additionalFiles = new List<string>();

    public Form1()
    {
        InitializeComponent();
        CheckInnoSetupInstalled();
    }

    private void CheckInnoSetupInstalled()
    {
        string[] possibleIsccPaths = new[]
        {
            @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
            @"C:\Program Files\Inno Setup 6\ISCC.exe",
            @"C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
            @"C:\Program Files\Inno Setup 5\ISCC.exe"
        };

        string? isccPath = possibleIsccPaths.FirstOrDefault(File.Exists);

        if (isccPath == null)
        {
            DialogResult result = MessageBox.Show(
                "Inno Setup이 설치되어 있지 않습니다.\n\n" +
                "설치파일을 생성하려면 Inno Setup을 먼저 설치해주세요.\n\n" +
                "다운로드 페이지로 이동하시겠습니까?\n" +
                "URL: https://jrsoftware.org/isdl.php",
                "Inno Setup 필요",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://jrsoftware.org/isdl.php",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"브라우저를 열 수 없습니다.\n\n직접 다음 URL로 접속해주세요:\nhttps://jrsoftware.org/isdl.php\n\n오류: {ex.Message}",
                        "오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }
    }

    private void btnBrowseProject_Click(object? sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "C# Project Files (*.csproj)|*.csproj";
            ofd.Title = "프로젝트 파일 선택";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtProjectPath.Text = ofd.FileName;
            }
        }
    }

    private void txtProjectPath_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void txtProjectPath_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            string file = files[0];
            // .csproj 파일만 허용
            if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                txtProjectPath.Text = file;
                LogMessage($"프로젝트 파일 추가됨 (Drag & Drop): {file}");
            }
            else
            {
                MessageBox.Show("C# 프로젝트 파일(.csproj)만 선택 가능합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void btnBrowseOutput_Click(object? sender, EventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "설치파일 출력 경로 선택";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = fbd.SelectedPath;
            }
        }
    }

    private async void btnBuild_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtProjectPath.Text))
        {
            MessageBox.Show("프로젝트 경로를 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(txtProjectPath.Text))
        {
            MessageBox.Show("프로젝트 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
        {
            MessageBox.Show("출력 경로를 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnBuild.Enabled = false;
        txtLog.Clear();
        LogMessage("=== 설치파일 생성 시작 ===");

        try
        {
            string outputDir = txtOutputPath.Text;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                LogMessage($"출력 디렉토리 생성: {outputDir}");
            }

            await BuildProjectAsync(txtProjectPath.Text, outputDir);

            LogMessage("=== 설치파일 생성 완료 ===");
            MessageBox.Show("설치파일이 성공적으로 생성되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"오류 발생: {ex.Message}");
            MessageBox.Show($"설치파일 생성 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnBuild.Enabled = true;
        }
    }

    private async Task BuildProjectAsync(string projectPath, string outputDir)
    {
        LogMessage($"\n프로젝트 빌드 중: {Path.GetFileName(projectPath)}");

        // 프로젝트 파일 분석
        string projectContent = "";
        try
        {
            projectContent = File.ReadAllText(projectPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"프로젝트 파일을 읽을 수 없습니다: {ex.Message}");
        }

        // .NET Framework 프로젝트 감지
        bool isNetFramework = IsNetFrameworkProject(projectContent);
        bool hasLicensesFile = projectContent.Contains("licenses.licx");

        // MSBuild 필요 조건:
        // 1. .NET Framework 프로젝트
        // 2. licenses.licx 파일이 있는 경우 (.NET Core/5+에서도 LC 작업은 MSBuild 필요)
        bool needsMSBuild = isNetFramework || hasLicensesFile;

        if (needsMSBuild)
        {
            if (isNetFramework)
            {
                LogMessage(".NET Framework 프로젝트 감지 - MSBuild.exe 사용");
            }
            if (hasLicensesFile)
            {
                LogMessage("licenses.licx 파일 감지 - MSBuild.exe 필요 (LC 작업)");
            }

            string? msbuildPath = FindMSBuildPath();
            if (msbuildPath != null)
            {
                LogMessage($"MSBuild 경로: {msbuildPath}");

                // 플랫폼 감지
                string platform = DetectPlatform(projectContent);
                string platformArg = "";

                if (!string.IsNullOrEmpty(platform))
                {
                    // Platform 값에 공백이 있으면 따옴표로 감싸기
                    if (platform.Contains(" "))
                    {
                        platformArg = $" /p:Platform=\"{platform}\"";
                    }
                    else
                    {
                        platformArg = $" /p:Platform={platform}";
                    }
                }

                LogMessage($"MSBuild 빌드 시작 (플랫폼: {(string.IsNullOrEmpty(platform) ? "기본값" : platform)})");

                // MSBuild 인자 구성
                // /restore: NuGet 패키지 복원
                // /p:Configuration=Release: Release 빌드
                // /v:normal: 자세한 로그 (오류 진단용)
                // /t:Rebuild: 전체 재빌드
                string msbuildArgs = $"\"{projectPath}\" /restore /p:Configuration=Release{platformArg} /v:normal /t:Rebuild";
                LogMessage($"MSBuild 명령: \"{msbuildPath}\" {msbuildArgs}");
                await RunProcessDirectlyAsync(msbuildPath, msbuildArgs);
            }
            else
            {
                LogMessage("경고: MSBuild를 찾을 수 없습니다.");
                LogMessage("dotnet build 시도 중... (.NET Framework 프로젝트는 실패할 수 있습니다)");
                string buildCommand = $"dotnet build \"{projectPath}\" -c Release";
                await RunCommandAsync(buildCommand);
            }
        }
        else
        {
            // .NET Core/5+ 프로젝트 - dotnet build 사용
            LogMessage(".NET Core/5+ 프로젝트 - dotnet build 사용");
            string buildCommand = $"dotnet build \"{projectPath}\" -c Release";
            await RunCommandAsync(buildCommand);
        }

        // 빌드 출력 경로 확인
        string projectDir = Path.GetDirectoryName(projectPath) ?? "";
        string releasePath = Path.Combine(projectDir, "bin", "Release");

        if (!Directory.Exists(releasePath))
        {
            throw new Exception($"Release 빌드 출력 경로를 찾을 수 없습니다: {releasePath}");
        }

        string buildOutput;

        // .NET Framework 프로젝트는 bin\Release\ 직접 사용할 수도 있음
        // .NET Core/5+는 bin\Release\net6.0\ 같은 하위 폴더 사용
        var frameworkDirs = Directory.GetDirectories(releasePath).OrderByDescending(d => d).ToList();

        if (frameworkDirs.Count > 0)
        {
            // 하위 폴더가 있으면 최신 프레임워크 폴더 사용
            buildOutput = frameworkDirs[0];
            LogMessage($"빌드 출력 경로: {buildOutput}");
        }
        else
        {
            // 하위 폴더가 없으면 Release 폴더 자체 사용 (.NET Framework 구형 프로젝트)
            var files = Directory.GetFiles(releasePath, "*.exe");
            if (files.Length > 0)
            {
                buildOutput = releasePath;
                LogMessage($"빌드 출력 경로 (.NET Framework): {buildOutput}");
            }
            else
            {
                throw new Exception($"빌드 출력 파일을 찾을 수 없습니다: {releasePath}");
            }
        }

        // 프로젝트 이름 추출
        string projectName = Path.GetFileNameWithoutExtension(projectPath);

        // 설치파일 생성을 위한 임시 디렉토리 생성
        string tempInstallerDir = Path.Combine(outputDir, $"{projectName}_Installer_Temp");
        if (Directory.Exists(tempInstallerDir))
        {
            Directory.Delete(tempInstallerDir, true);
        }
        Directory.CreateDirectory(tempInstallerDir);

        // 빌드 출력물을 임시 디렉토리로 복사
        CopyDirectory(buildOutput, tempInstallerDir);
        LogMessage($"빌드 출력물 복사 완료: {tempInstallerDir}");

        // DLL 파일 복사 (폴더 구조 유지)
        if (dllFiles.Count > 0)
        {
            string dllDestDir = Path.Combine(tempInstallerDir, "DllFiles");
            Directory.CreateDirectory(dllDestDir);

            // 공통 부모 디렉토리 찾기
            string? commonParent = FindCommonParentDirectory(dllFiles);

            foreach (string dllFile in dllFiles)
            {
                string relativePath;
                if (!string.IsNullOrEmpty(commonParent))
                {
                    // 공통 부모 디렉토리 기준으로 상대 경로 계산
                    relativePath = Path.GetRelativePath(commonParent, dllFile);
                }
                else
                {
                    // 공통 부모가 없으면 파일명만 사용
                    relativePath = Path.GetFileName(dllFile);
                }

                string destFile = Path.Combine(dllDestDir, relativePath);
                string? destFileDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destFileDir))
                {
                    Directory.CreateDirectory(destFileDir);
                }

                File.Copy(dllFile, destFile, true);
                LogMessage($"DLL 복사: {Path.GetFileName(dllFile)} -> {relativePath}");
            }
        }

        // 추가 파일 복사 (폴더 구조 유지)
        if (additionalFiles.Count > 0)
        {
            string additionalDestDir = Path.Combine(tempInstallerDir, "AdditionalFiles");
            Directory.CreateDirectory(additionalDestDir);

            // 공통 부모 디렉토리 찾기
            string? commonParent = FindCommonParentDirectory(additionalFiles);

            foreach (string addFile in additionalFiles)
            {
                string relativePath;
                if (!string.IsNullOrEmpty(commonParent))
                {
                    // 공통 부모 디렉토리 기준으로 상대 경로 계산
                    relativePath = Path.GetRelativePath(commonParent, addFile);
                }
                else
                {
                    // 공통 부모가 없으면 파일명만 사용
                    relativePath = Path.GetFileName(addFile);
                }

                string destFile = Path.Combine(additionalDestDir, relativePath);
                string? destFileDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destFileDir))
                {
                    Directory.CreateDirectory(destFileDir);
                }

                File.Copy(addFile, destFile, true);
                LogMessage($"추가 파일 복사: {Path.GetFileName(addFile)} -> {relativePath}");
            }
        }

        // Inno Setup 스크립트 생성 및 실행
        await CreateAndRunInnoSetupAsync(projectName, tempInstallerDir, outputDir);

        // 임시 디렉토리 삭제
        Directory.Delete(tempInstallerDir, true);
    }

    private async Task CreateAndRunInnoSetupAsync(string projectName, string sourceDir, string outputDir)
    {
        LogMessage($"설치파일 생성 중: {projectName}");

        string exeName = $"{projectName}.exe";

        // WiX Toolset 경로 확인
        string? wixPath = FindWixToolset();

        if (wixPath != null)
        {
            LogMessage("WiX Toolset을 사용하여 MSI 설치파일을 생성합니다.");
            await CreateWixInstallerAsync(projectName, sourceDir, outputDir, wixPath);
        }
        else
        {
            LogMessage("WiX Toolset이 설치되어 있지 않습니다.");

            // Inno Setup 컴파일러 경로 확인
            string[] possibleIsccPaths = new[]
            {
                @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
                @"C:\Program Files\Inno Setup 6\ISCC.exe",
                @"C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
                @"C:\Program Files\Inno Setup 5\ISCC.exe"
            };

            string? isccPath = possibleIsccPaths.FirstOrDefault(File.Exists);

            if (isccPath != null)
            {
                LogMessage("Inno Setup을 사용하여 설치파일을 생성합니다.");
                await CreateInnoSetupInstallerAsync(projectName, sourceDir, outputDir, isccPath);
            }
            else
            {
                LogMessage("설치파일 생성 도구가 없습니다. SFX 자동압축해제 실행파일을 생성합니다.");
                await CreateSfxInstallerAsync(projectName, sourceDir, outputDir);
            }
        }
    }

    private string? FindWixToolset()
    {
        // WiX Toolset v3/v4 경로 확인
        string[] possibleWixPaths = new[]
        {
            @"C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe",
            @"C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe",
            @"C:\Program Files\WiX Toolset v3.11\bin\candle.exe",
            @"C:\Program Files\WiX Toolset v3.14\bin\candle.exe"
        };

        foreach (var path in possibleWixPaths)
        {
            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path);
            }
        }

        // dotnet tool로 설치된 WiX 확인
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool list -g",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (output.Contains("wix"))
                {
                    return "dotnet-wix";
                }
            }
        }
        catch { }

        return null;
    }

    private bool IsNetFrameworkProject(string projectContent)
    {
        // .NET Framework 프로젝트 특징:
        // 1. TargetFrameworkVersion 태그 존재 (구형 프로젝트)
        if (projectContent.Contains("<TargetFrameworkVersion>"))
        {
            return true;
        }

        // 2. Microsoft.NET.Sdk.WindowsDesktop 확인 - 이건 .NET Core 3.0+ WinForms
        if (projectContent.Contains("Microsoft.NET.Sdk.WindowsDesktop"))
        {
            return false; // .NET Core/5+ WinForms
        }

        // 3. net4.x, net35, net20 등 체크 (net5.0, net6.0과 구분)
        if (System.Text.RegularExpressions.Regex.IsMatch(
                projectContent,
                @"<TargetFramework>net(4\d{1,2}|3[05]|20)</TargetFramework>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return true; // .NET Framework 2.0, 3.5, 4.x
        }

        // 4. 구형 프로젝트 형식 확인 (xmlns에 2003이 포함)
        if (projectContent.Contains("http://schemas.microsoft.com/developer/msbuild/2003"))
        {
            return true;
        }

        return false;
    }

    private string DetectPlatform(string projectContent)
    {
        // Release Configuration의 PlatformTarget 먼저 확인
        var releaseMatch = System.Text.RegularExpressions.Regex.Match(
            projectContent,
            @"<PropertyGroup[^>]*Condition[^>]*Release[^>]*>[\s\S]*?<PlatformTarget>([^<]+)</PlatformTarget>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (releaseMatch.Success)
        {
            string platform = releaseMatch.Groups[1].Value.Trim();
            if (platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
            {
                return "Any CPU";
            }
            return platform;
        }

        // Condition 없는 기본 Platform 찾기
        var defaultPlatformMatch = System.Text.RegularExpressions.Regex.Match(
            projectContent,
            @"<Platform\s+Condition[^>]*==\s*''\s*""[^>]*>([^<]+)</Platform>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (defaultPlatformMatch.Success && !defaultPlatformMatch.Groups[1].Value.Contains("$("))
        {
            string platform = defaultPlatformMatch.Groups[1].Value.Trim();
            if (platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
            {
                return "Any CPU";
            }
            return platform;
        }

        // 일반 PlatformTarget 찾기
        var platformTargetMatch = System.Text.RegularExpressions.Regex.Match(
            projectContent,
            @"<PlatformTarget>([^<]+)</PlatformTarget>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (platformTargetMatch.Success)
        {
            string platform = platformTargetMatch.Groups[1].Value.Trim();
            if (platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
            {
                return "Any CPU";
            }
            return platform;
        }

        // 기본값: Any CPU (대부분의 .NET 프로젝트 기본값)
        return "Any CPU";
    }

    private string? FindMSBuildPath()
    {
        // Visual Studio 2022 MSBuild 경로
        string[] possibleMSBuildPaths = new[]
        {
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        };

        foreach (var path in possibleMSBuildPaths)
        {
            if (File.Exists(path))
            {
                LogMessage($"MSBuild 발견: {path}");
                return path;
            }
        }

        LogMessage("MSBuild를 찾을 수 없습니다.");
        return null;
    }

    private async Task CreateWixInstallerAsync(string projectName, string sourceDir, string outputDir, string wixPath)
    {
        string exeName = $"{projectName}.exe";
        string wxsPath = Path.Combine(Path.GetTempPath(), $"{projectName}.wxs");
        string wixobjPath = Path.Combine(Path.GetTempPath(), $"{projectName}.wixobj");

        // 버전 정보 가져오기
        string version = "1.0.0";
        if (txtVersion.InvokeRequired)
        {
            txtVersion.Invoke(new Action(() => { version = txtVersion.Text; }));
        }
        else
        {
            version = txtVersion.Text;
        }

        string msiPath = Path.Combine(outputDir, $"{projectName}_{version}_Setup.msi");

        // GUID 생성
        string productGuid = Guid.NewGuid().ToString().ToUpper();
        string upgradeGuid = Guid.NewGuid().ToString().ToUpper();

        // 파일 목록 생성
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        var fileComponents = new System.Text.StringBuilder();
        int fileId = 1;

        foreach (var file in files)
        {
            string relativePath = Path.GetRelativePath(sourceDir, file);
            string fileName = Path.GetFileName(file);
            fileComponents.AppendLine($"          <Component Id='Component{fileId}' Guid='{Guid.NewGuid().ToString().ToUpper()}'>");
            fileComponents.AppendLine($"            <File Id='File{fileId}' Name='{fileName}' Source='{file}' KeyPath='yes' />");
            fileComponents.AppendLine($"          </Component>");
            fileId++;
        }

        // WiX 스크립트 생성
        string wxsContent = $@"<?xml version='1.0' encoding='windows-1252'?>
<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>
  <Product Name='{projectName}' Id='{productGuid}' UpgradeCode='{upgradeGuid}'
    Language='1042' Codepage='949' Version='1.0.0' Manufacturer='GreenPower'>

    <Package Id='*' Keywords='Installer' Description='{projectName} Installer'
      Comments='{projectName} Installer' Manufacturer='GreenPower'
      InstallerVersion='200' Languages='1042' Compressed='yes' SummaryCodepage='949' />

    <Media Id='1' Cabinet='Sample.cab' EmbedCab='yes' />

    <Directory Id='TARGETDIR' Name='SourceDir'>
      <Directory Id='ProgramFilesFolder' Name='PFiles'>
        <Directory Id='INSTALLDIR' Name='{projectName}'>
          {fileComponents}
        </Directory>
      </Directory>

      <Directory Id='DesktopFolder' Name='Desktop' />
      <Directory Id='ProgramMenuFolder' Name='Programs'>
        <Directory Id='ProgramMenuDir' Name='{projectName}' />
      </Directory>
    </Directory>

    <Feature Id='Complete' Level='1'>
      {string.Join(Environment.NewLine + "      ", Enumerable.Range(1, fileId - 1).Select(i => $"<ComponentRef Id='Component{i}' />"))}
      <ComponentRef Id='DesktopShortcut' />
      <ComponentRef Id='StartMenuShortcut' />
    </Feature>

    <DirectoryRef Id='DesktopFolder'>
      <Component Id='DesktopShortcut' Guid='{Guid.NewGuid().ToString().ToUpper()}'>
        <Shortcut Id='DesktopShortcut' Name='{projectName}'
          Description='{projectName} Application' Target='[INSTALLDIR]{exeName}'
          WorkingDirectory='INSTALLDIR' />
        <RemoveFolder Id='RemoveDesktopFolder' On='uninstall' />
        <RegistryValue Root='HKCU' Key='Software\{projectName}' Name='installed' Type='integer' Value='1' KeyPath='yes' />
      </Component>
    </DirectoryRef>

    <DirectoryRef Id='ProgramMenuDir'>
      <Component Id='StartMenuShortcut' Guid='{Guid.NewGuid().ToString().ToUpper()}'>
        <Shortcut Id='StartMenuShortcut' Name='{projectName}'
          Description='{projectName} Application' Target='[INSTALLDIR]{exeName}'
          WorkingDirectory='INSTALLDIR' />
        <RemoveFolder Id='RemoveProgramMenuDir' On='uninstall' />
        <RegistryValue Root='HKCU' Key='Software\{projectName}' Name='installed' Type='integer' Value='1' KeyPath='yes' />
      </Component>
    </DirectoryRef>

  </Product>
</Wix>";

        File.WriteAllText(wxsPath, wxsContent, System.Text.Encoding.UTF8);
        LogMessage($"WiX 스크립트 생성: {wxsPath}");

        // WiX 컴파일 (candle.exe)
        if (wixPath == "dotnet-wix")
        {
            await RunCommandAsync($"wix build \"{wxsPath}\" -o \"{msiPath}\"");
        }
        else
        {
            string candlePath = Path.Combine(wixPath, "candle.exe");
            string lightPath = Path.Combine(wixPath, "light.exe");

            await RunCommandAsync($"\"{candlePath}\" \"{wxsPath}\" -out \"{wixobjPath}\"");
            LogMessage("WiX 컴파일 완료");

            // WiX 링크 (light.exe)
            await RunCommandAsync($"\"{lightPath}\" \"{wixobjPath}\" -out \"{msiPath}\" -ext WixUIExtension -sval");
            LogMessage($"MSI 설치파일 생성 완료: {msiPath}");
        }

        // 임시 파일 삭제
        if (File.Exists(wxsPath)) File.Delete(wxsPath);
        if (File.Exists(wixobjPath)) File.Delete(wixobjPath);
    }

    private async Task CreateInnoSetupInstallerAsync(string projectName, string sourceDir, string outputDir, string isccPath)
    {
        string exeName = $"{projectName}.exe";
        string scriptPath = Path.Combine(Path.GetTempPath(), $"{projectName}_setup.iss");

        // 프로그램 이름 기반 고정 GUID 생성 (동일 프로그램은 항상 같은 GUID 사용)
        string appId = GenerateConsistentGuid(projectName);

        // Inno Setup 스크립트 생성
        // 주의:
        // 1. #define으로 변수 정의
        // 2. AppId는 {} 안에 GUID 포함
        // 3. {autopf}는 Program Files 자동 선택
        // 4. WizardStyle=modern 추가
        // 5. 한글 지원을 위해 Korean.isl 추가
        // Inno Setup 스크립트에서는 백슬래시를 그대로 사용 가능
        // 또는 슬래시(/)로 대체 가능
        string outputDirForScript = outputDir.Replace("\\", "/");
        string sourceDirForScript = sourceDir.Replace("\\", "/");

        // 버전 및 옵션 정보 가져오기
        string version = "1.0.0";
        string defaultInstallPath = "{autopf}\\{#MyAppName}";
        bool deleteFilesOnUninstall = true;
        bool overwriteFiles = true;

        if (txtVersion.InvokeRequired)
        {
            txtVersion.Invoke(new Action(() =>
            {
                version = txtVersion.Text;
                defaultInstallPath = txtDefaultInstallPath.Text.Replace("{AppName}", "{#MyAppName}");
                deleteFilesOnUninstall = chkDeleteFilesOnUninstall.Checked;
                overwriteFiles = chkOverwriteFiles.Checked;
            }));
        }
        else
        {
            version = txtVersion.Text;
            defaultInstallPath = txtDefaultInstallPath.Text.Replace("{AppName}", "{#MyAppName}");
            deleteFilesOnUninstall = chkDeleteFilesOnUninstall.Checked;
            overwriteFiles = chkOverwriteFiles.Checked;
        }

        // 복사 위치 경로 변환 (Inno Setup 형식으로)
        string dllDestInnoPath = ConvertToInnoSetupPath(txtDllDestPath.Text);
        string additionalDestInnoPath = ConvertToInnoSetupPath(txtAdditionalFilesDestPath.Text);

        // [Tasks] 섹션 생성 (설치 시 사용자가 선택 가능)
        var tasksSection = new System.Text.StringBuilder();
        bool hasConfigFiles = dllFiles.Count > 0 || additionalFiles.Count > 0;

        if (hasConfigFiles)
        {
            // 기본값 설정 (빌더에서 설정한 값 기준)
            string overwriteChecked = overwriteFiles ? "" : " unchecked";
            string deleteChecked = deleteFilesOnUninstall ? "" : " unchecked";

            tasksSection.AppendLine($@"Name: ""overwriteconfig""; Description: ""기존 설정/DLL 파일이 있어도 새 파일로 덮어쓰기""; Flags:{overwriteChecked}");
            tasksSection.AppendLine($@"Name: ""deleteconfig""; Description: ""프로그램 제거 시 설정/DLL 파일도 함께 삭제""; Flags:{deleteChecked}");
        }

        // [Files] 섹션 생성
        var filesSection = new System.Text.StringBuilder();

        // 기본 빌드 출력물 (DllFiles, AdditionalFiles 폴더 제외)
        filesSection.AppendLine($@"Source: ""{sourceDirForScript}/*""; DestDir: ""{{app}}""; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: ""DllFiles,AdditionalFiles""");

        // DLL 파일들 (폴더 구조 유지) - Tasks 조건부 처리
        if (dllFiles.Count > 0)
        {
            // 덮어쓰기 선택 시
            filesSection.AppendLine($@"Source: ""{sourceDirForScript}/DllFiles/*""; DestDir: ""{dllDestInnoPath}""; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: overwriteconfig");
            // 덮어쓰지 않기 선택 시 (기존 파일 유지)
            filesSection.AppendLine($@"Source: ""{sourceDirForScript}/DllFiles/*""; DestDir: ""{dllDestInnoPath}""; Flags: onlyifdoesntexist recursesubdirs createallsubdirs; Tasks: not overwriteconfig");
        }

        // 추가 파일들 (폴더 구조 유지) - Tasks 조건부 처리
        if (additionalFiles.Count > 0)
        {
            // 덮어쓰기 선택 시
            filesSection.AppendLine($@"Source: ""{sourceDirForScript}/AdditionalFiles/*""; DestDir: ""{additionalDestInnoPath}""; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: overwriteconfig");
            // 덮어쓰지 않기 선택 시 (기존 파일 유지)
            filesSection.AppendLine($@"Source: ""{sourceDirForScript}/AdditionalFiles/*""; DestDir: ""{additionalDestInnoPath}""; Flags: onlyifdoesntexist recursesubdirs createallsubdirs; Tasks: not overwriteconfig");
        }

        // [UninstallDelete] 섹션 생성 (Tasks 조건부)
        var uninstallDeleteSection = new System.Text.StringBuilder();
        if (dllFiles.Count > 0)
        {
            uninstallDeleteSection.AppendLine($@"Type: filesandordirs; Name: ""{dllDestInnoPath}""; Tasks: deleteconfig");
        }
        if (additionalFiles.Count > 0)
        {
            uninstallDeleteSection.AppendLine($@"Type: filesandordirs; Name: ""{additionalDestInnoPath}""; Tasks: deleteconfig");
        }

        string script = $@"#define MyAppName ""{projectName}""
#define MyAppVersion ""{version}""
#define MyAppPublisher ""GreenPower""
#define MyAppExeName ""{exeName}""

[Setup]
AppId={{{{{appId}}}}}
AppName={{#MyAppName}}
AppVersion={{#MyAppVersion}}
AppPublisher={{#MyAppPublisher}}
DefaultDirName={defaultInstallPath}
DefaultGroupName={{#MyAppName}}
UninstallDisplayName={{#MyAppName}}
OutputDir={outputDirForScript}
OutputBaseFilename={{#MyAppName}}_{{#MyAppVersion}}_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: ""korean""; MessagesFile: ""compiler:Languages\Korean.isl""

{(tasksSection.Length > 0 ? $@"[Tasks]
{tasksSection}" : "")}

[Files]
{filesSection}

[Icons]
Name: ""{{group}}\{{#MyAppName}}""; Filename: ""{{app}}\{{#MyAppExeName}}""
Name: ""{{autodesktop}}\{{#MyAppName}}""; Filename: ""{{app}}\{{#MyAppExeName}}""

[Run]
Filename: ""{{app}}\{{#MyAppExeName}}""; Description: ""{{cm:LaunchProgram,{{#StringChange(MyAppName, '&', '&&')}}}}""; Flags: nowait postinstall skipifsilent

{(uninstallDeleteSection.Length > 0 ? $@"[UninstallDelete]
{uninstallDeleteSection}" : "")}

[Code]
var
  UninstallProgressPage: TOutputProgressWizardPage;

procedure InitializeWizard;
begin
  UninstallProgressPage := CreateOutputProgressPage('기존 버전 제거', '이전 버전을 제거하는 중입니다...');
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  UninstallString: String;
  UninstallKey: String;
  InstallPath: String;
  UninstallExe: String;
begin
  Result := True;
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{{{{appId}}}}}_is1';

  // 기존 버전이 설치되어 있는지 확인 (레지스트리 조회)
  if RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', UninstallString) or
     RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', UninstallString) then
  begin
    // UninstallString에서 따옴표 제거
    UninstallString := RemoveQuotes(UninstallString);

    // 사용자에게 기존 버전 제거 여부 확인
    if MsgBox('이 프로그램의 이전 버전이 이미 설치되어 있습니다.' + #13#10 + #13#10 +
              '기존 버전: ' + UninstallString + #13#10 + #13#10 +
              '기존 버전을 자동으로 제거한 후 새 버전을 설치합니다.' + #13#10 + #13#10 +
              '계속하시겠습니까?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // 제거 진행 화면 표시
      UninstallProgressPage.Show;
      try
        UninstallProgressPage.SetText('기존 버전을 제거하는 중입니다...', '잠시만 기다려주세요. (unins000.exe 실행 중)');
        UninstallProgressPage.SetProgress(0, 100);

        // unins000.exe 실행 (SILENT 모드)
        if Exec(UninstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        begin
          UninstallProgressPage.SetProgress(100, 100);
          Sleep(1000);  // 파일 정리 대기
          // 제거 성공
          Result := True;
          MsgBox('기존 버전이 성공적으로 제거되었습니다.' + #13#10 + #13#10 +
                 '이어서 새 버전 설치를 진행합니다.', mbInformation, MB_OK);
        end
        else
        begin
          // 제거 실패 시 메시지 표시
          MsgBox('기존 프로그램을 제거하는 중 오류가 발생했습니다.' + #13#10 +
                 '종료 코드: ' + IntToStr(ResultCode) + #13#10 + #13#10 +
                 '수동으로 프로그램을 제거한 후 다시 설치해주세요.', mbError, MB_OK);
          Result := False;
        end;
      finally
        UninstallProgressPage.Hide;
      end;
    end
    else
    begin
      // 사용자가 '아니오'를 선택한 경우 설치 취소
      Result := False;
    end;
  end;
end;
";

        // 출력 디렉토리가 존재하는지 확인하고 생성
        if (!Directory.Exists(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                LogMessage($"출력 디렉토리 생성: {outputDir}");
            }
            catch (Exception ex)
            {
                LogMessage($"출력 디렉토리 생성 실패: {ex.Message}");
                throw new Exception($"출력 디렉토리를 생성할 수 없습니다: {outputDir}\n오류: {ex.Message}");
            }
        }

        File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);
        LogMessage($"Inno Setup 스크립트 생성: {scriptPath}");
        LogMessage($"스크립트 내용:");
        LogMessage($"-------------------");
        LogMessage(script);
        LogMessage($"-------------------");

        try
        {
            // Inno Setup 실행 - cmd.exe를 거치지 않고 직접 실행
            LogMessage($"Inno Setup 컴파일러 실행: {isccPath}");
            LogMessage($"스크립트 파일: {scriptPath}");
            await RunProcessDirectlyAsync(isccPath, $"\"{scriptPath}\"");

            // 생성된 설치파일 확인
            string installerPath = Path.Combine(outputDir, $"{projectName}_{version}_Setup.exe");
            if (File.Exists(installerPath))
            {
                LogMessage($"✓ 설치파일 생성 완료: {installerPath}");
                LogMessage($"  파일 크기: {new FileInfo(installerPath).Length / 1024 / 1024:F2} MB");
            }
            else
            {
                LogMessage($"경고: 설치파일이 예상 경로에 없습니다: {installerPath}");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Inno Setup 실행 오류: {ex.Message}");

            // 스크립트를 디버깅을 위해 출력 디렉토리에도 저장
            try
            {
                string debugScriptPath = Path.Combine(outputDir, $"{projectName}_setup_debug.iss");
                File.Copy(scriptPath, debugScriptPath, true);
                LogMessage($"디버그용 스크립트 저장: {debugScriptPath}");
            }
            catch { }

            throw;
        }
        finally
        {
            // 스크립트 파일 삭제 (디버깅 시에는 주석처리 가능)
            // if (File.Exists(scriptPath))
            // {
            //     File.Delete(scriptPath);
            // }
        }
    }

    private async Task CreateSfxInstallerAsync(string projectName, string sourceDir, string outputDir)
    {
        string exeName = $"{projectName}.exe";

        // 버전 정보 가져오기
        string version = "1.0.0";
        if (txtVersion.InvokeRequired)
        {
            txtVersion.Invoke(new Action(() => { version = txtVersion.Text; }));
        }
        else
        {
            version = txtVersion.Text;
        }

        // 7-Zip 경로 확인
        string[] possible7zPaths = new[]
        {
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe"
        };

        string? sevenZipPath = possible7zPaths.FirstOrDefault(File.Exists);

        if (sevenZipPath != null)
        {
            LogMessage("7-Zip을 사용하여 SFX 설치파일을 생성합니다.");

            // 임시 압축 파일 생성
            string tempArchive = Path.Combine(Path.GetTempPath(), $"{projectName}_temp.7z");
            string sfxPath = Path.Combine(outputDir, $"{projectName}_{version}_Setup.exe");

            // 7z 압축 파일 생성
            await RunCommandAsync($"\"{sevenZipPath}\" a -t7z \"{tempArchive}\" \"{sourceDir}\\*\" -mx9");
            LogMessage("압축 파일 생성 완료");

            // SFX 모듈 경로
            string sfxModule = Path.Combine(Path.GetDirectoryName(sevenZipPath) ?? "", "7zSD.sfx");
            if (!File.Exists(sfxModule))
            {
                sfxModule = Path.Combine(Path.GetDirectoryName(sevenZipPath) ?? "", "7z.sfx");
            }

            if (File.Exists(sfxModule))
            {
                // SFX 설정 파일 생성
                string configPath = Path.Combine(Path.GetTempPath(), $"{projectName}_config.txt");
                string configContent = $@";!@Install@!UTF-8!
Title=""{projectName} 설치""
BeginPrompt=""이 프로그램은 {projectName}을(를) 설치합니다.\n\n계속하시겠습니까?""
RunProgram=""CreateDesktopShortcut.bat""
;!@InstallEnd@!";

                File.WriteAllText(configPath, configContent);

                // 바로가기 생성 배치 파일 추가
                string batContent = $@"@echo off
echo {projectName} 바로가기 생성 중...
set SCRIPT=""%TEMP%\CreateShortcut.vbs""
echo Set oWS = WScript.CreateObject(""WScript.Shell"") >> %SCRIPT%
echo sLinkFile = ""%USERPROFILE%\Desktop\{projectName}.lnk"" >> %SCRIPT%
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> %SCRIPT%
echo oLink.TargetPath = ""%~dp0{exeName}"" >> %SCRIPT%
echo oLink.WorkingDirectory = ""%~dp0"" >> %SCRIPT%
echo oLink.Save >> %SCRIPT%
cscript /nologo %SCRIPT%
del %SCRIPT%
";
                string batPath = Path.Combine(sourceDir, "CreateDesktopShortcut.bat");
                File.WriteAllText(batPath, batContent);

                // 배치 파일을 압축 파일에 추가
                await RunCommandAsync($"\"{sevenZipPath}\" a -t7z \"{tempArchive}\" \"{batPath}\"");

                // SFX 실행파일 생성 (바이너리 결합)
                using (FileStream output = new FileStream(sfxPath, FileMode.Create))
                {
                    using (FileStream sfx = new FileStream(sfxModule, FileMode.Open))
                    {
                        await sfx.CopyToAsync(output);
                    }
                    using (FileStream config = new FileStream(configPath, FileMode.Open))
                    {
                        await config.CopyToAsync(output);
                    }
                    using (FileStream archive = new FileStream(tempArchive, FileMode.Open))
                    {
                        await archive.CopyToAsync(output);
                    }
                }

                LogMessage($"SFX 설치파일 생성 완료: {sfxPath}");

                // 임시 파일 삭제
                File.Delete(tempArchive);
                File.Delete(configPath);
                File.Delete(batPath);
            }
            else
            {
                LogMessage("SFX 모듈을 찾을 수 없습니다. ZIP 파일로 대체합니다.");
                await CreateZipFallbackAsync(projectName, sourceDir, outputDir);
            }
        }
        else
        {
            LogMessage("7-Zip이 설치되어 있지 않습니다. ZIP 파일로 대체합니다.");
            await CreateZipFallbackAsync(projectName, sourceDir, outputDir);
        }
    }

    private async Task CreateZipFallbackAsync(string projectName, string sourceDir, string outputDir)
    {
        // 버전 정보 가져오기
        string version = "1.0.0";
        if (txtVersion.InvokeRequired)
        {
            txtVersion.Invoke(new Action(() => { version = txtVersion.Text; }));
        }
        else
        {
            version = txtVersion.Text;
        }

        string capturedVersion = version;

        await Task.Run(() =>
        {
            string exeName = $"{projectName}.exe";
            string zipPath = Path.Combine(outputDir, $"{projectName}_{capturedVersion}_Portable.zip");

            // 바로가기 생성 배치 파일 추가
            string batContent = $@"@echo off
echo {projectName} 바로가기 생성 중...
set SCRIPT=""%TEMP%\CreateShortcut.vbs""
echo Set oWS = WScript.CreateObject(""WScript.Shell"") >> %SCRIPT%
echo sLinkFile = ""%USERPROFILE%\Desktop\{projectName}.lnk"" >> %SCRIPT%
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> %SCRIPT%
echo oLink.TargetPath = ""%~dp0{exeName}"" >> %SCRIPT%
echo oLink.WorkingDirectory = ""%~dp0"" >> %SCRIPT%
echo oLink.Save >> %SCRIPT%
cscript /nologo %SCRIPT%
del %SCRIPT%
echo 바로가기가 바탕화면에 생성되었습니다.
pause
";
            string batPath = Path.Combine(sourceDir, "CreateDesktopShortcut.bat");
            File.WriteAllText(batPath, batContent);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(sourceDir, zipPath);
            LogMessage($"ZIP 파일 생성 완료: {zipPath}");
        });
    }

    private async Task RunCommandAsync(string command)
    {
        await Task.Run(() =>
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // 한글 인코딩 설정 (CP949)
            // 주의: Encoding.RegisterProvider(CodePagesEncodingProvider.Instance) 필요
            try
            {
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(949);
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding(949);
            }
            catch
            {
                // CP949를 사용할 수 없으면 UTF-8 사용
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
            }

            var outputLines = new System.Collections.Concurrent.ConcurrentBag<string>();
            var errorLines = new System.Collections.Concurrent.ConcurrentBag<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputLines.Add(e.Data);
                    LogMessage($"[OUT] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorLines.Add(e.Data);
                    LogMessage($"[ERR] {e.Data}");
                }
            };

            LogMessage($"[CMD] {command}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            LogMessage($"[EXIT CODE] {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                string errorDetail = string.Join(Environment.NewLine, errorLines);
                string outputDetail = string.Join(Environment.NewLine, outputLines);

                string fullError = $"명령 실행 실패 (종료 코드: {process.ExitCode})" + Environment.NewLine +
                                   $"명령: {command}" + Environment.NewLine +
                                   $"표준 출력:{Environment.NewLine}{outputDetail}" + Environment.NewLine +
                                   $"오류 출력:{Environment.NewLine}{errorDetail}";

                throw new Exception(fullError);
            }
        });
    }

    private async Task RunProcessDirectlyAsync(string exePath, string arguments)
    {
        await Task.Run(() =>
        {
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // 한글 인코딩 설정
            try
            {
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(949);
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding(949);
            }
            catch
            {
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
            }

            var outputLines = new System.Collections.Concurrent.ConcurrentBag<string>();
            var errorLines = new System.Collections.Concurrent.ConcurrentBag<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputLines.Add(e.Data);
                    LogMessage($"[OUT] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorLines.Add(e.Data);
                    LogMessage($"[ERR] {e.Data}");
                }
            };

            LogMessage($"[EXEC] {exePath} {arguments}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            LogMessage($"[EXIT CODE] {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                string errorDetail = string.Join(Environment.NewLine, errorLines);
                string outputDetail = string.Join(Environment.NewLine, outputLines);

                string fullError = $"프로세스 실행 실패 (종료 코드: {process.ExitCode})" + Environment.NewLine +
                                   $"실행 파일: {exePath}" + Environment.NewLine +
                                   $"인수: {arguments}" + Environment.NewLine +
                                   $"표준 출력:{Environment.NewLine}{outputDetail}" + Environment.NewLine +
                                   $"오류 출력:{Environment.NewLine}{errorDetail}";

                throw new Exception(fullError);
            }
        });
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private void LogMessage(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(new Action(() =>
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }));
        }
        else
        {
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }

    // ========== 프로그램 이름 기반 고정 GUID 생성 ==========
    private string GenerateConsistentGuid(string projectName)
    {
        // 프로그램 이름을 기반으로 항상 같은 GUID 생성
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectName));
            Guid guid = new Guid(hash);
            return guid.ToString().ToUpper();
        }
    }

    // ========== 공통 부모 디렉토리 찾기 ==========
    private string? FindCommonParentDirectory(List<string> filePaths)
    {
        if (filePaths.Count == 0)
            return null;

        if (filePaths.Count == 1)
            return Path.GetDirectoryName(filePaths[0]);

        // 모든 파일의 디렉토리 경로를 가져옴
        var directories = filePaths.Select(f => Path.GetDirectoryName(f) ?? "").ToList();

        // 첫 번째 디렉토리를 기준으로 공통 부분 찾기
        string commonPath = directories[0];

        foreach (var dir in directories.Skip(1))
        {
            commonPath = GetCommonPath(commonPath, dir);
            if (string.IsNullOrEmpty(commonPath))
                return null;
        }

        return commonPath;
    }

    private string GetCommonPath(string path1, string path2)
    {
        var parts1 = path1.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parts2 = path2.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var commonParts = new List<string>();
        int minLength = Math.Min(parts1.Length, parts2.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (string.Equals(parts1[i], parts2[i], StringComparison.OrdinalIgnoreCase))
            {
                commonParts.Add(parts1[i]);
            }
            else
            {
                break;
            }
        }

        if (commonParts.Count == 0)
            return "";

        return string.Join(Path.DirectorySeparatorChar.ToString(), commonParts);
    }

    // ========== 경로 변환 ==========
    private string ConvertToInnoSetupPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "{app}";
        }

        // [INSTALLDIR] -> {app}
        if (path.Equals("[INSTALLDIR]", StringComparison.OrdinalIgnoreCase))
        {
            return "{app}";
        }

        // [INSTALLDIR]\SubDir -> {app}\SubDir
        if (path.StartsWith("[INSTALLDIR]\\", StringComparison.OrdinalIgnoreCase))
        {
            string subPath = path.Substring("[INSTALLDIR]\\".Length);
            return $"{{app}}\\{subPath}";
        }

        if (path.StartsWith("[INSTALLDIR]/", StringComparison.OrdinalIgnoreCase))
        {
            string subPath = path.Substring("[INSTALLDIR]/".Length);
            return $"{{app}}/{subPath}";
        }

        // 절대 경로는 그대로 반환
        return path;
    }

    // ========== DLL 파일 관리 ==========
    private void btnAddDll_Click(object? sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "All Files (*.*)|*.*";
            ofd.Title = "DLL 및 관련 파일 선택";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    if (!dllFiles.Contains(file))
                    {
                        dllFiles.Add(file);
                        lstDllFiles.Items.Add(file);
                        LogMessage($"DLL 관련 파일 추가됨: {file}");
                    }
                }
            }
        }
    }

    private void btnRemoveDll_Click(object? sender, EventArgs e)
    {
        if (lstDllFiles.SelectedItems.Count > 0)
        {
            // 선택된 항목들을 임시 리스트에 저장 (역순으로 제거하기 위해)
            var selectedItems = lstDllFiles.SelectedItems.Cast<string>().ToList();

            foreach (string item in selectedItems)
            {
                int index = lstDllFiles.Items.IndexOf(item);
                if (index >= 0 && index < dllFiles.Count)
                {
                    string removedFile = dllFiles[index];
                    dllFiles.RemoveAt(index);
                    lstDllFiles.Items.RemoveAt(index);
                    LogMessage($"DLL 제거됨: {removedFile}");
                }
            }
        }
        else
        {
            MessageBox.Show("제거할 DLL 파일을 선택해주세요.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btnBrowseDllDest_Click(object? sender, EventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "DLL 파일 복사 위치 선택";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtDllDestPath.Text = fbd.SelectedPath;
            }
        }
    }

    private void lstDllFiles_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void lstDllFiles_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (string file in files)
            {
                if (!dllFiles.Contains(file))
                {
                    dllFiles.Add(file);
                    lstDllFiles.Items.Add(file);
                    LogMessage($"DLL 관련 파일 추가됨 (Drag & Drop): {file}");
                }
            }
        }
    }

    // ========== 일반 파일 관리 ==========
    private void btnAddAdditionalFile_Click(object? sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "All Files (*.*)|*.*|Config Files (*.config)|*.config|Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json";
            ofd.Title = "추가 파일 선택";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    if (!additionalFiles.Contains(file))
                    {
                        additionalFiles.Add(file);
                        lstAdditionalFiles.Items.Add(file);
                        LogMessage($"파일 추가됨: {file}");
                    }
                }
            }
        }
    }

    private void btnRemoveAdditionalFile_Click(object? sender, EventArgs e)
    {
        if (lstAdditionalFiles.SelectedItems.Count > 0)
        {
            // 선택된 항목들을 임시 리스트에 저장 (역순으로 제거하기 위해)
            var selectedItems = lstAdditionalFiles.SelectedItems.Cast<string>().ToList();

            foreach (string item in selectedItems)
            {
                int index = lstAdditionalFiles.Items.IndexOf(item);
                if (index >= 0 && index < additionalFiles.Count)
                {
                    string removedFile = additionalFiles[index];
                    additionalFiles.RemoveAt(index);
                    lstAdditionalFiles.Items.RemoveAt(index);
                    LogMessage($"파일 제거됨: {removedFile}");
                }
            }
        }
        else
        {
            MessageBox.Show("제거할 파일을 선택해주세요.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btnBrowseAdditionalFilesDest_Click(object? sender, EventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "추가 파일 복사 위치 선택";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtAdditionalFilesDestPath.Text = fbd.SelectedPath;
            }
        }
    }

    private void lstAdditionalFiles_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void lstAdditionalFiles_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (string file in files)
            {
                if (!additionalFiles.Contains(file))
                {
                    additionalFiles.Add(file);
                    lstAdditionalFiles.Items.Add(file);
                    LogMessage($"파일 추가됨 (Drag & Drop): {file}");
                }
            }
        }
    }

    // ========== 기본 설치 경로 찾아보기 ==========
    private void btnBrowseDefaultInstallPath_Click(object? sender, EventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "기본 설치 경로 선택\n(Inno Setup 특수 경로도 직접 입력 가능합니다)";

            // 현재 경로가 실제 경로인 경우에만 초기 경로로 설정
            if (!string.IsNullOrEmpty(txtDefaultInstallPath.Text) &&
                !txtDefaultInstallPath.Text.StartsWith("{") &&
                Directory.Exists(txtDefaultInstallPath.Text))
            {
                fbd.SelectedPath = txtDefaultInstallPath.Text;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtDefaultInstallPath.Text = fbd.SelectedPath;
            }
        }
    }

    // ========== 정보보기 ==========
    private void btnAbout_Click(object? sender, EventArgs e)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Assembly 정보 가져오기
        string appName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "InstallBuilder";
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? assembly.GetName().Version?.ToString() ?? "1.0.0";
        string authors = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
        string copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        string description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

        // Authors 속성은 csproj에서 AssemblyMetadataAttribute로 전달됨
        var authorsAttr = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                  .FirstOrDefault(a => a.Key == "Authors");
        string author = authorsAttr?.Value ?? "";

        var aboutInfo = new System.Text.StringBuilder();
        aboutInfo.AppendLine($"프로그램: {appName}");
        aboutInfo.AppendLine($"버전: {version}");

        if (!string.IsNullOrWhiteSpace(author))
            aboutInfo.AppendLine($"제작자: {author}");

        if (!string.IsNullOrWhiteSpace(authors) && authors != author)
            aboutInfo.AppendLine($"회사: {authors}");

        if (!string.IsNullOrWhiteSpace(copyright))
            aboutInfo.AppendLine($"저작권: {copyright}");

        if (!string.IsNullOrWhiteSpace(description))
            aboutInfo.AppendLine($"설명: {description}");

        MessageBox.Show(
            aboutInfo.ToString(),
            "정보",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}
