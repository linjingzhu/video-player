# video-player

Windows desktop **영상 플레이어**. Local files only. No store, DRM, accounts, or streaming.

## Confirmed P0

- **Containers:** MP4, MKV, AVI, WMV, MOV
- **Video:** H.264, HEVC, VP9, AV1, MPEG-4 ASP
- **Audio:** AAC, AC-3, E-AC-3, MP3, FLAC, Opus, PCM
- **Subtitles:** SRT, SMI, embedded in MKV/MOV. Same-folder same-stem: `stem.srt`, `stem.smi`, `stem.ko.srt`. `.en` is suggested as the secondary (top) track. **보기 > 자막** opens the sheet. **CC** toggles the primary (bottom) track only.
- **Decoder:** libmpv (FFmpeg). Media Foundation alone is not used
- **GPU:** D3D11VA / DXVA when possible. Hardware failure falls back to software and keeps playing. Status bar shows **failures only** (unsupported codec name, HW fallback)
- **Out of scope:** ProRes, DNxHD, camera RAW, encrypted WMV, DVD/ISO
- **Playback:** speed 0.5–2.0x (resets to 1.0 on restart), ±10초, seek, wheel volume
- **Resume key:** path + size. Last 10 seconds marks the current title **complete only** and does not seek the next episode
- **Window:** remember size. Next launch is windowed. Open and drag-drop
- **jumpSeconds:** global AppData key reserved for v1.5 (integer 1–60, default 10). No settings UI in P0

## Confirmed P0 shell

- Sidebar **closed by default** as a **36px** toggle rail. When open: one resume item + recent series
- Menus: **파일 | 보기** only. Series panel is under 보기
- Video uses remaining width. No large centered play icon. Click the video surface to toggle play/pause
- Transport: prev | **-10초** | play | **+10초** | next (icon only) | seek | volume | **1.0x** | **CC** | fullscreen
- Time overlay sits above the transport. **다음 화** is an end-region video overlay, not a transport label
- Skip capsules (**인트로 / 리캡 / 크레딧 건너뛰기**) sit bottom-right on the video only. Auto countdown **취소 (N)** runs only when an in-season marker exists (chapter aliases stay manual). No external skip database
- Status bar is a dashed slot and **hidden when idle**. Failure line only: **미지원** / **SW 폴백**
- No always-on-top pin

## P1 series

- Drill-down: show → season → episode (folder = season)
- Columns: 회차 / 제목 / 진행. Sort by episode only. No explorer file table
- Auto next **ON** by default. Natural end waits 3 seconds (cancel) and shows an end-region **다음 화** CTA
- No playlist button

## Build

```bash
dotnet test tests/VideoPlayer.Tests/VideoPlayer.Tests.csproj
# Windows:
dotnet build src/VideoPlayer.App/VideoPlayer.App.csproj -c Release
```

Place `libmpv-2.dll` beside `VideoPlayer.exe`. The shell still opens if it is missing.

## License

MIT
