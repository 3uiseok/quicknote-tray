# QuickNote

![sheep](sheep-preview.png)

Windows 전용 편의 도구. 전역 단축키 한 번으로 새 `.md` / `.txt` 파일을 만든다.

탐색기의 "새로 만들기"는 새 폴더 만들기는 편하지만, 문서를 자주 새로 만들어야 할 때는 메뉴를 여러 번 거쳐야 하고 `.md`는 목록에 없다. 메모·계획서·결과서처럼 문서를 많이 만드는 경우에 쓰려고 만들었다.

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

`QuickNote.exe` 더블클릭. 받은 exe에 "Windows PC 보호" 창이 뜨면 속성 > 차단 해제, 또는 추가 정보 > 실행.
