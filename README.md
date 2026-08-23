# video-player

Windows desktop **영상 플레이어** (SeriesOn-inspired). Local files only. No store, DRM, accounts, or streaming.

## Product (v1)

- **Containers:** MP4, MKV, AVI, WMV, MOV
- **Video:** H.264, HEVC, VP9, AV1, MPEG-4 ASP
- **Audio:** AAC, AC-3, E-AC-3, MP3, FLAC, Opus, PCM
- **Subtitles:** SRT, SMI, plus embedded in MKV/MOV. Same-folder same-name sidecars auto-load
- **Decoder:** libmpv (FFmpeg). Media Foundation alone is not used
- **GPU:** DXVA / D3D11VA when possible. Hardware failure falls back to software and **keeps playing**. Status bar always shows HW|SW and codec names
- **Out of scope:** ProRes, DNxHD, camera RAW, encrypted WMV, DVD/ISO, seek thumbnails

Unsupported files show the codec/container name and are **not** added to Recent.

## Playback and series

- Speed 0.5–2.0x (global; resets to 1.0 on restart)
- ±10s, frame step, wheel volume, contain/cover
- Folder = season; episode sort by `SxxExx` or numeric filename; auto next
- Resume key = **path + size**. Saved on exit, pause, and episode change
- Last 10 seconds of an episode records the **next episode at 0s** (confirmed)
- 95% watched also marks complete (estimate)

## Shell

Matches the attached wireframes:

- **A Main:** title 영상 플레이어 · 파일/재생/시리즈/보기/도움 · 최근/시리즈 · video · transport · status
- **B Fullscreen:** chrome hides after 3s idle, stays when paused, Esc back, 다음 화. Opens on the window's current monitor. Next launch is windowed
- **C Series:** two-level folder tree + table (회차 / 파일명 / 길이 / 진행)

Also: playlist save/load, drag-and-drop, Explorer context-open (`--register-explorer`), taskbar ±10/play, media keys, window size memory, always-on-top, single instance (hand-off to the existing window).

## Why WPF + libmpv

WinUI 3 cannot run on the Linux CI/cloud VM used to develop this repo. The portable rules live in `VideoPlayer.Core` (net8.0) and are tested here. The Windows UI is **WPF** hosting **libmpv**, which is FFmpeg-based and does not rely on Media Foundation as the decoder.

## Build

```bash
# Portable tests (Linux or Windows)
dotnet test tests/VideoPlayer.Tests/VideoPlayer.Tests.csproj

# Windows app (requires Windows + libmpv-2.dll next to VideoPlayer.exe)
dotnet build src/VideoPlayer.App/VideoPlayer.App.csproj -c Release
```

Place a Windows `libmpv-2.dll` (from an mpv/libmpv build) beside the executable. The shell still opens if the DLL is missing; playback then reports that libmpv was not found.

### Explorer verb

```text
VideoPlayer.exe --register-explorer
```

Registers *영상 플레이어로 재생* for the supported extensions under the current user.

## Layout

- `src/VideoPlayer.Core` — resume, series, subtitles, path safety, shell model
- `src/VideoPlayer.App` — WPF windows A/B/C and libmpv host
- `tests/VideoPlayer.Tests` — defensive unit tests only

## License

MIT
