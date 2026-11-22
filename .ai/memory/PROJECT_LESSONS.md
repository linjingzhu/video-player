---
doc_id: ai-project-lessons
version: 1.0.0
canonical_path: .ai/memory/PROJECT_LESSONS.md
updated: 2026-11-22
---

# Project Lessons

## Policy adoption baseline

- Evidence: linjingzhu/video-player at commit 88511907dae97ebc69e4ef5833565e904032b4af; shared source commit 94e808cc78d8ca194a8e20272a395551be3066db (ai-dev-rule 3.0.0).
- The repository default branch is stable; derive future work from this configured base rather than assuming main.
- GitHub Actions were removed in the 2026-09-25 policy adoption, then restored on 2026-11-22 for Windows CI (`.github/workflows/ci.yml`) and release packaging (`.github/workflows/release.yml`). Do not remove those workflows without a new instruction. A green workflow answers "did every step exit zero", not "playback works".
- A successful .NET build does not verify libmpv playback, device projection, GPU/HDR handling or the Windows shell. `tests/VideoPlayer.Smoke` only checks that libmpv loads with `vo=null` and that the main window can be constructed and closed. A SKIP from that probe is an environment limit, not a playback pass.
- Policy structure is separate evidence from application compilation, tests and runtime behavior. Do not report unrun application checks as successful.

## Preserved project memory

No prior repository entry instructions were present at the recorded baseline. No historical implementation lessons are invented here.

Only add future lessons with a concrete file, test, error or measured observation as evidence.

## Native runtime pins (2026-11-22)

- Evidence: PE import tables of shinchiro `mpv-dev-x86_64-20261111-git-bcde6b93cc` `libmpv-2.dll` and `ffmpeg-x86_64-git-cd4e96eb3` `ffmpeg.exe`. Both list `vulkan-1.dll` in the regular import directory, and the delay-import directory is empty. `scripts/package-release.ps1` therefore also downloads LunarG Vulkan Runtime 1.4.321.0 `vulkan-1.dll` (zip sha256 `f8ba79303face4b72a2f302f1a46e8c918995b54fbf24c0c9fc6c3d3ef525dca`). Archive checksums: libmpv `273c079b11ea5575822f972756f4c9a71149a5c10130353abeb8586738938452`, ffmpeg `b943ccb7b7760fd5d2c449ebcbedaac5ce85b64437a9d877af0761ba402ed118`.
- The dev archive contains one `libmpv-2.dll`. The ffmpeg archive contains one `ffmpeg.exe`. Clip export needs the `libwebp` encoder and a `gif` muxer; the package script runs `ffmpeg.exe -encoders` and `-formats` and fails the build when either is missing.
