# QuickNote

![sheep](sheep-preview.png)

Windows 전용 편의 도구. 전역 단축키 한 번으로 새 `.md` / `.txt` 파일을 만든다.

탐색기의 "새로 만들기"는 새 폴더 만들기는 편하지만, 문서를 자주 새로 만들어야 할 때는 메뉴를 여러 번 거쳐야 하고 `.md`는 목록에 없다. 메모·계획서·결과서처럼 문서를 많이 만드는 경우에 쓰려고 만들었다.

## 설치 (명령 한 줄)

cmd 또는 PowerShell에 붙여 넣는다. 관리자 권한은 필요 없다.

```
powershell -ExecutionPolicy Bypass -c "irm https://raw.githubusercontent.com/3uiseok/quicknote-tray/main/install.ps1 | iex"
```

- 최신 Release의 `QuickNote.zip`을 받아 `%LOCALAPPDATA%\Programs\QuickNote`에 풀고, 시작 메뉴에 바로가기를 만든 뒤 실행한다
- 같은 명령을 다시 실행하면 최신판으로 바꾼다 (실행 중인 QuickNote는 먼저 종료)
- 설치 위치를 바꾸려면 먼저 `set QUICKNOTE_DIR=D:\Tools\QuickNote` (PowerShell은 `$env:QUICKNOTE_DIR="D:\Tools\QuickNote"`)
- 지우기: 트레이 메뉴에서 종료 → 위 폴더와 시작 메뉴의 `QuickNote` 바로가기 삭제. "Windows 시작 시 실행"을 켰다면 먼저 끈다

직접 받으려면 [Releases](https://github.com/3uiseok/quicknote-tray/releases/latest)에서 `QuickNote.zip`을 받아 원하는 폴더에 푼다.

## "Windows의 PC 보호" 창이 뜰 때

서명하지 않은 exe라서, 브라우저로 받은 파일을 처음 실행하면 SmartScreen이 막는다. 위 설치 명령은 받은 뒤 차단 표시를 지우므로 보통 뜨지 않는다. 브라우저로 받았다면 다음 중 하나를 한다.

1. **압축 풀기 전에** (권장) PowerShell에서
   ```powershell
   Unblock-File -Path "$env:USERPROFILE\Downloads\QuickNote.zip"
   ```
2. 이미 풀었다면 폴더 안 파일 전부
   ```powershell
   Get-ChildItem "C:\경로\QuickNote" -Recurse | Unblock-File
   ```
3. 탐색기에서 zip(또는 exe) 오른쪽 클릭 > 속성 > 아래쪽 **차단 해제** 체크 > 확인
4. 그래도 창이 뜨면 **추가 정보** > **실행**

### "애플리케이션 제어 정책에서 이 파일을 차단했습니다"가 뜰 때

PC 보호 창이 아니라 이 메시지가 뜨면 **스마트 앱 컨트롤**(Windows 보안 > 앱 및 브라우저 컨트롤)이 켜져 있는 것이다. 이 기능은 파일별 해제가 없어 위 방법이 통하지 않는다.

- Release의 exe는 스마트 앱 컨트롤이 켜진 PC에서 실행되는 것을 확인했다. 직접 빌드한 exe는 해시가 달라져 막힐 수 있으니 Release 판을 쓴다
- 그래도 막히면 스마트 앱 컨트롤을 끄는 방법뿐이다. 한 번 끄면 Windows를 다시 설치하기 전에는 다시 켤 수 없으니 신중히 정한다

## 기능

| 단축키 | 동작 |
|---|---|
| `Ctrl+Alt+N` | 새 `.md` 파일 (`# 파일이름` 제목 한 줄 포함) |
| `Ctrl+Alt+T` | 새 `.txt` 파일 |

- 탐색기가 앞에 있으면 **그 폴더(활성 탭)** 에, 바탕화면이면 바탕화면에, 아니면 기본 폴더(`문서\QuickNotes`)에 만든다
- 만든 파일을 탐색기에서 선택해 보여 주고, 설정에 따라 바로 연다
- 파일 이름: `새 메모_yyyyMMdd_HHmmss` 기본. 앞부분과 형식(날짜 앞/뒤)을 바꿀 수 있다. 같은 이름이 있으면 ` (2)`를 붙인다
- 트레이 상주. X·최소화는 트레이로 숨김, 종료는 트레이 메뉴에서
- "Windows 시작 시 트레이로 실행" 옵션 (`HKCU\...\Run`에 `/tray`로 등록)
- 설정은 exe 옆 `quicknote.ini`에 저장

## 빌드

추가 설치 없이 Windows에 들어 있는 .NET Framework 4.x의 `csc.exe`로 빌드한다.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

`MakeIcon.cs`로 `sheep.ico`를 만든 뒤 `QuickNote.exe`에 아이콘으로 넣는다. `make-icon.ps1`은 같은 아이콘을 PowerShell로 만드는 예전 방식이다.

## 실행

`QuickNote.exe` 더블클릭. 설치는 필요 없다(.NET Framework 4.x, Windows 10/11 기본 포함). PC 보호 창이 뜨면 위의 해제 방법을 따른다.
