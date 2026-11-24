# Release zip에 yt-dlp.exe — 2026-11-24

질문: Release zip의 `Ieseo.exe` 옆에 공식 yt-dlp 특정 버전이 SHA-256 검증을 거쳐 들어가는가.

## 변경

`scripts/package-release.ps1`가 공식 릴리스 `2026.11.23`의 `yt-dlp.exe`를 받아 SHA-256을 확인한 뒤 zip 루트에 `yt-dlp.exe`로 복사한다. 생성되는 `THIRD-PARTY.txt`에 URL과 해시를 적는다. README의 zip 구성, Open URL 문장, Release 워크플로 노트, `YtDlpLocator` 주석을 그에 맞춘다. 찾기 순서(실행 파일 옆, 그다음 PATH)와 재생 옵션은 바꾸지 않았다.

대본 저장 메뉴와 yt2txt 연동은 이 변경에 없다.

## 핀

- URL: `https://github.com/yt-dlp/yt-dlp/releases/download/2026.11.23/yt-dlp.exe`
- SHA-256: `622ed48023cac1daa52a3bf3a6ac38f03514de214b5f0fa069389fd0f6d17e5c`
- 같은 값이 릴리스 `SHA2-256SUMS`의 `yt-dlp.exe` 줄, GitHub 자산 digest, 로컬 `sha256sum`과 일치했다. 이 일치는 "받은 파일이 그 릴리스의 exe인가"에 답한다. zip이 Windows에서 만들어지는가는 답하지 않는다.

## 로컬 검증

- `dotnet test tests/VideoPlayer.Tests/VideoPlayer.Tests.csproj -c Release`: 339 passed, 0 failed, 0 skipped. 질문: Core 테스트가 통과하는가.
- `dotnet build src/VideoPlayer.App/VideoPlayer.App.csproj -c Release -p:EnableWindowsTargeting=true`: 0 warning, 0 error. 산출은 Linux에서 `Ieseo.dll`. 질문: 프로젝트가 컴파일되는가. Windows `Ieseo.exe`가 생겼는가는 답하지 않는다.

NOT VERIFIED: Windows에서 `package-release.ps1`가 zip을 만들고 그 안에 `yt-dlp.exe`가 `Ieseo.exe` 옆에 있는지. 유튜브 재생. 실기(Windows) 확인.
