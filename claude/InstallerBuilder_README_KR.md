# InstallerBuilder 프로젝트 문서

## 프로젝트 개요

InstallerBuilder는 여러 C# 프로젝트를 위한 Windows 설치 파일(EXE)을 자동으로 생성하는 GUI 애플리케이션입니다.

### 주요 기능

- **다중 프로젝트 지원**: 사용자가 원하는 만큼 프로젝트를 추가하여 각각 설치 파일 생성
- **.NET Framework 및 .NET Core/5+ 지원**: 자동 감지 및 적절한 빌드 도구 선택
- **Inno Setup 통합**: 전문적인 Windows 설치 프로그램 생성
- **DevExpress 라이센스 지원**: licenses.licx 파일을 포함한 프로젝트 처리
- **바탕화면 바로가기 생성**: 설치 시 자동으로 바로가기 생성

## 기술 스택

- **개발 환경**: .NET 9.0 Windows Forms
- **지원 프로젝트 타입**:
  - .NET Framework 2.0 ~ 4.8.x
  - .NET Core 3.0+
  - .NET 5.0+
- **빌드 도구**:
  - MSBuild (Visual Studio 2019/2022)
  - dotnet CLI
- **설치 파일 생성**: Inno Setup 5/6

## 시스템 요구사항

### 필수 요구사항
- Windows 10 이상
- .NET 9.0 Runtime (실행용)
- Visual Studio 2019 이상 (.NET Framework 프로젝트 빌드용)

### 선택 요구사항
- Inno Setup 5 또는 6 (설치 파일 생성용)

## 아키텍처 및 설계

### 프로젝트 타입 감지 로직

```csharp
private bool IsNetFrameworkProject(string projectContent)
{
    // 1. TargetFrameworkVersion 태그 존재 (구형 프로젝트)
    if (projectContent.Contains("<TargetFrameworkVersion>"))
        return true;

    // 2. Microsoft.NET.Sdk.WindowsDesktop 확인 - .NET Core 3.0+ WinForms
    if (projectContent.Contains("Microsoft.NET.Sdk.WindowsDesktop"))
        return false;

    // 3. net4.x, net35, net20 등 체크
    if (Regex.IsMatch(projectContent, @"<TargetFramework>net(4\d{1,2}|3[05]|20)</TargetFramework>"))
        return true;

    // 4. 구형 프로젝트 형식 확인
    if (projectContent.Contains("http://schemas.microsoft.com/developer/msbuild/2003"))
        return true;

    return false;
}
```

### 빌드 전략

#### MSBuild 사용 조건
1. .NET Framework 프로젝트
2. licenses.licx 파일이 포함된 프로젝트 (모든 .NET 버전)

**이유**: LC(License Compiler) 작업은 .NET Core의 `dotnet build`에서 지원되지 않으며, .NET Framework MSBuild에서만 실행 가능

#### MSBuild 명령어
```bash
MSBuild.exe "<프로젝트경로>" /restore /p:Configuration=Release /p:Platform="<플랫폼>" /v:normal /t:Rebuild
```

**주요 옵션**:
- `/restore`: NuGet 패키지 자동 복원
- `/p:Configuration=Release`: Release 빌드
- `/p:Platform="<플랫폼>"`: 플랫폼 지정 (Any CPU, x86, x64)
- `/v:normal`: 상세 로그 출력
- `/t:Rebuild`: 전체 재빌드

#### dotnet build 사용
.NET Core/5+ 프로젝트 (licenses.licx 없는 경우)
```bash
dotnet build "<프로젝트경로>" -c Release
```

### 플랫폼 감지 로직

```csharp
private string DetectPlatform(string projectContent)
{
    // 1. Release Configuration의 PlatformTarget 확인
    var releaseMatch = Regex.Match(
        projectContent,
        @"<PropertyGroup[^>]*Condition[^>]*Release[^>]*>[\s\S]*?<PlatformTarget>([^<]+)</PlatformTarget>"
    );

    if (releaseMatch.Success)
        return releaseMatch.Groups[1].Value == "AnyCPU" ? "Any CPU" : releaseMatch.Groups[1].Value;

    // 2. 일반 PlatformTarget 확인
    // 3. 기본값: "Any CPU"
}
```

**중요**: "Any CPU"와 같이 공백이 포함된 플랫폼 이름은 MSBuild 인자로 전달 시 따옴표로 감싸야 함

### Inno Setup 스크립트 생성

```inno
[Setup]
AppId={{GUID}}
AppName={프로젝트명}
AppVersion=1.0
AppPublisher=GreenPower
DefaultDirName={autopf}\{프로젝트명}
OutputDir=<출력경로>
OutputBaseFilename={프로젝트명}_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "<빌드출력>/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{프로젝트명}"; Filename: "{app}\{프로젝트명}.exe"
Name: "{autodesktop}\{프로젝트명}"; Filename: "{app}\{프로젝트명}.exe"

[Run]
Filename: "{app}\{프로젝트명}.exe"; Description: "프로그램 실행"; Flags: nowait postinstall skipifsilent
```

## 주요 해결 과제 및 솔루션

### 1. 인코딩 문제 (CP949)

**문제**: .NET Core는 기본적으로 CP949 인코딩을 포함하지 않음
```
No data is available for encoding 949
```

**해결책**:
```csharp
// Program.cs
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// InstallerBuilder.csproj
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
```

### 2. MSBuild 경로 문제

**문제**: cmd.exe를 통한 실행 시 경로에 공백이 있으면 파싱 오류 발생

**해결책**: Process를 직접 실행 (cmd.exe 없이)
```csharp
private async Task RunProcessDirectlyAsync(string exePath, string arguments)
{
    process.StartInfo.FileName = exePath;
    process.StartInfo.Arguments = arguments;
    process.StartInfo.UseShellExecute = false;
    // cmd.exe 사용 안 함
}
```

### 3. Inno Setup 경로 문제

**문제**: 백슬래시 이스케이프 문제
```
D:\\__work\\__src -> Inno Setup 파싱 오류
```

**해결책**: 슬래시(/)로 변환
```csharp
string outputDirForScript = outputDir.Replace("\\", "/");
// D:/__work/__src
```

### 4. licenses.licx LC 작업 오류

**문제**: .NET Core/5+에서 `dotnet build` 사용 시
```
error MSB4803: "LC" 작업은 MSBuild의 .NET Core 버전에서 지원되지 않습니다.
```

**해결책**: licenses.licx 파일이 있는 모든 프로젝트는 MSBuild 사용
```csharp
bool needsMSBuild = isNetFramework || hasLicensesFile;
```

### 5. MSB1008 오류 - 프로젝트 지정 오류

**문제**: Platform 값에 공백이 있을 때 MSBuild 파싱 오류
```
/p:Platform=Any CPU  // 잘못됨 - "CPU"가 별도 인자로 인식
```

**해결책**: 공백이 있는 경우 따옴표로 감싸기
```csharp
if (platform.Contains(" "))
    platformArg = $" /p:Platform=\"{platform}\"";
else
    platformArg = $" /p:Platform={platform}";
```

## 프로젝트 구조

```
ModuleCycler_2025/
├── InstallerBuilder/
│   ├── Form1.cs              # 메인 로직
│   ├── Form1.Designer.cs     # UI 디자인
│   ├── Program.cs            # 진입점, 인코딩 설정
│   └── InstallerBuilder.csproj
├── install/                  # 설치 파일 출력 경로
└── installer_cmd.txt         # 요구사항 문서
```

## 사용 방법

### 1. InstallerBuilder 실행
```bash
dotnet run --project InstallerBuilder/InstallerBuilder.csproj
# 또는
InstallerBuilder.exe
```

### 2. 프로젝트 추가
1. "프로젝트 경로" 텍스트박스에 .csproj 파일 경로 입력
2. "찾아보기..." 버튼으로 파일 선택 가능
3. "프로젝트 추가" 버튼 클릭
4. 필요한 만큼 반복

### 3. 출력 경로 설정
- 기본값: `D:\Installer`
- "찾아보기..." 버튼으로 변경 가능

### 4. 설치 파일 생성
- "설치파일 생성" 버튼 클릭
- 로그 창에서 진행 상황 확인

## 로그 출력 포맷

```
프로젝트 빌드 중: CANDBEditor.csproj
.NET Framework 프로젝트 감지 - MSBuild.exe 사용
licenses.licx 파일 감지 - MSBuild.exe 필요 (LC 작업)
MSBuild 경로: C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
MSBuild 빌드 시작 (플랫폼: Any CPU)
MSBuild 명령: "C:\...\MSBuild.exe" "D:\...\CANDBEditor.csproj" /restore /p:Configuration=Release /p:Platform="Any CPU" /v:normal /t:Rebuild
[EXEC] C:\...\MSBuild.exe ...
[OUT] 빌드 출력 라인
[ERR] 오류 출력 라인
[EXIT CODE] 0
빌드 출력 경로: D:\...\bin\Release\net8.0-windows
```

## 지원 프로젝트 목록

### 현재 프로젝트 (installer_cmd.txt)
1. **GreenPowerCycler** - 메인 배터리 사이클러 프로그램
2. **ScheduleEditor** - 스케줄 편집기 (.NET Framework + DevExpress)
3. **CyclerDataAnalyzer** - 데이터 분석 도구
4. **CANDBEditor** - CAN 데이터베이스 편집기 (.NET 8.0 + DevExpress)

## 설치 파일 출력

생성되는 파일:
```
D:\Installer\
├── GreenPowerCycler_Setup.exe
├── ScheduleEditor_Setup.exe
├── CyclerDataAnalyzer_Setup.exe
└── CANDBEditor_Setup.exe
```

각 설치 파일은:
- 바탕화면 바로가기 생성
- 시작 메뉴 항목 생성
- 프로그램 추가/제거에 등록
- 한글 설치 인터페이스 제공

## 문제 해결

### MSBuild를 찾을 수 없음
**증상**: "MSBuild를 찾을 수 없습니다" 메시지

**해결책**:
1. Visual Studio 2019 또는 2022 설치
2. "Desktop development with .NET" 워크로드 설치
3. MSBuild 경로 확인:
   - VS 2022: `C:\Program Files\Microsoft Visual Studio\2022\{Edition}\MSBuild\Current\Bin\MSBuild.exe`
   - Edition: Community, Professional, Enterprise

### Inno Setup을 찾을 수 없음
**증상**: "Inno Setup을 사용할 수 없습니다" 메시지

**해결책**:
1. [Inno Setup 다운로드](https://jrsoftware.org/isdl.php)
2. 기본 경로에 설치:
   - `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

### licenses.licx 관련 오류
**증상**: MSB4803 오류

**원인**: dotnet build로 빌드 시도

**자동 해결**: InstallerBuilder가 자동으로 MSBuild 사용

## 향후 개선 사항

1. **WiX Toolset 지원**: MSI 파일 생성 (현재 구현되어 있으나 미사용)
2. **병렬 빌드**: 여러 프로젝트 동시 빌드
3. **빌드 캐시**: 변경되지 않은 프로젝트 재빌드 방지
4. **버전 관리**: 자동 버전 증가
5. **서명 지원**: 코드 서명 통합

## 라이센스 및 기여

- **프로젝트**: GreenPower Cycler Suite
- **개발**: Claude Code + 사용자
- **버전**: 1.0
- **날짜**: 2025년

## 참고 자료

- [MSBuild 명령줄 참조](https://learn.microsoft.com/ko-kr/visualstudio/msbuild/msbuild-command-line-reference)
- [Inno Setup 문서](https://jrsoftware.org/ishelp/)
- [.NET CLI 문서](https://learn.microsoft.com/ko-kr/dotnet/core/tools/)
- [DevExpress License Compiler](https://docs.devexpress.com/GeneralInformation/2035/licensing)
