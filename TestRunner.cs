using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace InstallerBuilder;

public class TestRunner
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== InstallerBuilder 테스트 시작 ===");
        Console.WriteLine();

        // 테스트 프로젝트 경로
        string testProjectPath = @"D:\__work\__src\gitea\ModuleCycler_2025\TestInstallerApp\TestInstallerApp.csproj";
        string outputPath = @"D:\__work\__src\gitea\ModuleCycler_2025\install";

        if (!File.Exists(testProjectPath))
        {
            Console.WriteLine($"오류: 테스트 프로젝트를 찾을 수 없습니다: {testProjectPath}");
            return;
        }

        Console.WriteLine($"테스트 프로젝트: {testProjectPath}");
        Console.WriteLine($"출력 경로: {outputPath}");
        Console.WriteLine();

        try
        {
            // 1. 프로젝트 빌드
            Console.WriteLine("1. Release 빌드 수행 중...");
            await BuildProjectAsync(testProjectPath);
            Console.WriteLine("   ✓ 빌드 완료");
            Console.WriteLine();

            // 2. 빌드 출력 확인
            string projectDir = Path.GetDirectoryName(testProjectPath) ?? "";
            string releasePath = Path.Combine(projectDir, "bin", "Release");

            if (!Directory.Exists(releasePath))
            {
                Console.WriteLine($"   ✗ Release 빌드 출력 경로를 찾을 수 없습니다: {releasePath}");
                return;
            }

            var frameworkDirs = Directory.GetDirectories(releasePath);
            if (frameworkDirs.Length == 0)
            {
                Console.WriteLine($"   ✗ 빌드 출력 폴더를 찾을 수 없습니다");
                return;
            }

            string buildOutput = frameworkDirs[0];
            Console.WriteLine($"2. 빌드 출력 경로: {buildOutput}");

            var files = Directory.GetFiles(buildOutput);
            Console.WriteLine($"   파일 개수: {files.Length}");
            foreach (var file in files.Take(5))
            {
                Console.WriteLine($"   - {Path.GetFileName(file)}");
            }
            if (files.Length > 5)
            {
                Console.WriteLine($"   ... 외 {files.Length - 5}개");
            }
            Console.WriteLine();

            // 3. 설치 도구 확인
            Console.WriteLine("3. 설치 도구 확인 중...");

            bool hasInnoSetup = CheckInnoSetup();
            Console.WriteLine($"   Inno Setup: {(hasInnoSetup ? "설치됨" : "없음")}");

            bool hasWix = CheckWixToolset();
            Console.WriteLine($"   WiX Toolset: {(hasWix ? "설치됨" : "없음")}");

            bool has7Zip = Check7Zip();
            Console.WriteLine($"   7-Zip: {(has7Zip ? "설치됨" : "없음")}");
            Console.WriteLine();

            // 4. 설치파일 생성 시뮬레이션
            if (hasInnoSetup || hasWix || has7Zip)
            {
                Console.WriteLine("4. 설치파일 생성이 가능합니다.");
                if (hasWix)
                {
                    Console.WriteLine("   → WiX Toolset을 사용하여 MSI 파일 생성 예정");
                }
                else if (hasInnoSetup)
                {
                    Console.WriteLine("   → Inno Setup을 사용하여 EXE 파일 생성 예정");
                }
                else if (has7Zip)
                {
                    Console.WriteLine("   → 7-Zip SFX를 사용하여 자동압축해제 EXE 생성 예정");
                }
            }
            else
            {
                Console.WriteLine("4. 설치 도구가 없습니다. ZIP 파일로 대체됩니다.");
            }
            Console.WriteLine();

            // 5. Inno Setup 스크립트 샘플 생성 테스트
            if (hasInnoSetup)
            {
                Console.WriteLine("5. Inno Setup 스크립트 샘플 생성 중...");
                string sampleScript = GenerateSampleInnoScript("TestInstallerApp", buildOutput, outputPath);
                string sampleScriptPath = Path.Combine(outputPath, "sample_test.iss");

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                File.WriteAllText(sampleScriptPath, sampleScript, System.Text.Encoding.UTF8);
                Console.WriteLine($"   ✓ 샘플 스크립트 생성: {sampleScriptPath}");
                Console.WriteLine();
                Console.WriteLine("   스크립트 내용:");
                Console.WriteLine("   -------------------");
                foreach (var line in sampleScript.Split(Environment.NewLine).Take(20))
                {
                    Console.WriteLine($"   {line}");
                }
                Console.WriteLine("   ...");
                Console.WriteLine();
            }

            Console.WriteLine("=== 모든 테스트 완료 ===");
            Console.WriteLine();
            Console.WriteLine("GUI 프로그램을 실행하려면:");
            Console.WriteLine(@"start """" ""D:\__work\__src\gitea\ModuleCycler_2025\InstallerBuilder\bin\Release\net9.0-windows\InstallerBuilder.exe""");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류 발생: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task BuildProjectAsync(string projectPath)
    {
        var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"build \"{projectPath}\" -c Release";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"빌드 실패: {error}");
        }
    }

    private static bool CheckInnoSetup()
    {
        string[] paths = new[]
        {
            @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
            @"C:\Program Files\Inno Setup 6\ISCC.exe",
            @"C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
            @"C:\Program Files\Inno Setup 5\ISCC.exe"
        };
        return paths.Any(File.Exists);
    }

    private static bool CheckWixToolset()
    {
        string[] paths = new[]
        {
            @"C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe",
            @"C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe",
            @"C:\Program Files\WiX Toolset v3.11\bin\candle.exe",
            @"C:\Program Files\WiX Toolset v3.14\bin\candle.exe"
        };
        return paths.Any(File.Exists);
    }

    private static bool Check7Zip()
    {
        string[] paths = new[]
        {
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe"
        };
        return paths.Any(File.Exists);
    }

    private static string GenerateSampleInnoScript(string projectName, string sourceDir, string outputDir)
    {
        string exeName = $"{projectName}.exe";
        string appId = Guid.NewGuid().ToString().ToUpper();

        return $@"#define MyAppName ""{projectName}""
#define MyAppVersion ""1.0""
#define MyAppPublisher ""GreenPower""
#define MyAppExeName ""{exeName}""

[Setup]
AppId={{{{{appId}}}}}
AppName={{#MyAppName}}
AppVersion={{#MyAppVersion}}
AppPublisher={{#MyAppPublisher}}
DefaultDirName={{autopf}}\{{#MyAppName}}
DefaultGroupName={{#MyAppName}}
OutputDir={outputDir.Replace("\\", "\\\\")}
OutputBaseFilename={{#MyAppName}}_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: ""korean""; MessagesFile: ""compiler:Languages\Korean.isl""

[Files]
Source: ""{sourceDir.Replace("\\", "\\\\")}\*""; DestDir: ""{{app}}""; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: ""{{group}}\{{#MyAppName}}""; Filename: ""{{app}}\{{#MyAppExeName}}""
Name: ""{{autodesktop}}\{{#MyAppName}}""; Filename: ""{{app}}\{{#MyAppExeName}}""

[Run]
Filename: ""{{app}}\{{#MyAppExeName}}""; Description: ""{{cm:LaunchProgram,{{#StringChange(MyAppName, '&', '&&')}}}}""; Flags: nowait postinstall skipifsilent
";
    }
}
