# Version 1.1.3 작업 내역

## 작업 일자
2026-01-12

## 주요 변경 사항

### 1. 프로그램 기본 설치 경로 설정 기능 추가
- UI에 "기본 설치 경로" 입력란 추가
- 기본값: `{autopf}\{AppName}` (Program Files 자동 선택)
- 폴더 찾아보기 버튼 추가로 사용자 편의성 향상
- Inno Setup 특수 경로 직접 입력 가능

### 2. Config/DLL 파일 삭제 옵션
- "제거 시 config/dll 파일도 함께 삭제" 체크박스 추가
- 설치 시 사용자가 선택 가능하도록 Inno Setup [Tasks] 섹션 구현
- 기본값은 빌더에서 설정, 최종 사용자가 설치 시 변경 가능

### 3. Config/DLL 파일 덮어쓰기 옵션
- "설치 시 config/dll 파일 덮어쓰기" 체크박스 추가
- 설치 시 사용자가 선택 가능
- 체크 시: `ignoreversion` 플래그 사용
- 체크 해제 시: `onlyifdoesntexist` 플래그 사용

### 4. 설치 파일명 및 프로그램 이름 분리
- **설치 파일명**: `프로젝트명_버전_Setup.exe` (버전 포함)
  - 예: `GreenPowerCycler_1.0.0_Setup.exe`
- **설치된 프로그램 이름**: `프로젝트명` (버전 제외)
  - 제어판 표시: `GreenPowerCycler`
  - 버전 정보는 별도 열에 표시
- `UninstallDisplayName` 설정으로 제어판 표시 이름 제어

### 5. 같은 프로그램 재설치 시 자동 감지 및 제거
- 프로그램 이름 기반 고정 GUID 생성 (MD5 해시 사용)
- 같은 프로그램은 항상 같은 AppId 사용
- 기존 버전 자동 감지 및 제거 확인
- unins000.exe를 통한 제거 프로세스
- 제거 진행 상황 표시 및 완료 후 자동으로 설치 진행

### 6. 폴더 구조 유지하여 파일 복사
- config 폴더의 하위 경로 구조 유지
- 공통 부모 디렉토리 자동 감지
- 상대 경로 기반 복사
- Inno Setup에서 `recursesubdirs createallsubdirs` 플래그 사용

### 7. UI 개선 사항

#### 프로젝트 경로 드래그 앤 드롭
- .csproj 파일을 텍스트박스로 드래그 앤 드롭 가능
- 유효성 검사 (확장자 체크)

#### 파일 리스트 다중 선택
- DLL 파일 리스트: `SelectionMode.MultiExtended`
- 추가 파일 리스트: `SelectionMode.MultiExtended`
- Ctrl + 클릭, Shift + 클릭으로 여러 파일 선택
- 선택된 파일 일괄 삭제 가능

#### 기본 설치 경로 찾아보기 버튼
- 폴더 선택 다이얼로그 제공
- Inno Setup 특수 경로도 직접 입력 가능

### 8. Inno Setup 스크립트 개선

#### [Tasks] 섹션 추가
```pascal
[Tasks]
Name: "overwriteconfig"; Description: "기존 설정/DLL 파일이 있어도 새 파일로 덮어쓰기"
Name: "deleteconfig"; Description: "프로그램 제거 시 설정/DLL 파일도 함께 삭제"
```

#### [Code] 섹션 개선
- 제거 진행 상황 표시 페이지 추가
- unins000.exe 실행 시 `/SILENT /NORESTART /SUPPRESSMSGBOXES` 옵션 사용
- 오류 발생 시 종료 코드 표시
- 제거 완료 후 1초 대기 (파일 정리)

#### [UninstallDelete] 섹션
- Tasks 조건부로 파일 삭제
- 사용자가 선택한 경우에만 삭제

## 기술적 구현 세부사항

### 고정 GUID 생성
```csharp
private string GenerateConsistentGuid(string projectName)
{
    using (var md5 = System.Security.Cryptography.MD5.Create())
    {
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectName));
        Guid guid = new Guid(hash);
        return guid.ToString().ToUpper();
    }
}
```

### 공통 부모 디렉토리 찾기
```csharp
private string? FindCommonParentDirectory(List<string> filePaths)
{
    // 모든 파일의 공통 부모 디렉토리를 찾아 상대 경로 유지
}
```

### 다중 파일 삭제
```csharp
var selectedItems = lstDllFiles.SelectedItems.Cast<string>().ToList();
foreach (string item in selectedItems)
{
    // 선택된 모든 항목 제거
}
```

## 파일 변경 사항

### 수정된 파일
- `Form1.cs`: 주요 비즈니스 로직 구현
- `Form1.Designer.cs`: UI 컨트롤 추가 및 레이아웃 조정

### 주요 메서드 추가
- `GenerateConsistentGuid()`: 프로그램 이름 기반 GUID 생성
- `FindCommonParentDirectory()`: 공통 부모 디렉토리 찾기
- `GetCommonPath()`: 두 경로의 공통 부분 찾기
- `btnBrowseDefaultInstallPath_Click()`: 기본 설치 경로 찾아보기
- `txtProjectPath_DragEnter()`, `txtProjectPath_DragDrop()`: 프로젝트 경로 드래그 앤 드롭

### UI 컨트롤 추가
- `lblDefaultInstallPath`: 기본 설치 경로 레이블
- `txtDefaultInstallPath`: 기본 설치 경로 입력란
- `btnBrowseDefaultInstallPath`: 기본 설치 경로 찾아보기 버튼
- `chkDeleteFilesOnUninstall`: 제거 시 파일 삭제 체크박스
- `chkOverwriteFiles`: 덮어쓰기 체크박스

## 테스트 항목

### 기본 기능
- [x] 프로젝트 드래그 앤 드롭 (.csproj 파일만 허용)
- [x] 기본 설치 경로 설정 및 폴더 찾아보기
- [x] DLL/추가 파일 다중 선택 및 일괄 삭제
- [x] 설치 파일 생성 (버전 포함된 파일명)

### 설치 프로세스
- [x] 최초 설치 시 정상 작동
- [x] 같은 프로그램 재설치 시 기존 버전 감지
- [x] 제거 확인 메시지 표시
- [x] 제거 진행 상황 표시
- [x] 제거 완료 후 자동으로 설치 진행
- [x] 제어판에 프로그램 1개만 표시 (중복 없음)

### 파일 처리
- [x] 폴더 구조 유지하여 파일 복사
- [x] 덮어쓰기 옵션 선택 가능
- [x] 제거 시 파일 삭제 옵션 선택 가능

## 알려진 이슈
없음

## 향후 개선 사항
- 설치 완료 후 자동 실행 옵션
- 다국어 지원
- 설치 경로 유효성 검사 강화
