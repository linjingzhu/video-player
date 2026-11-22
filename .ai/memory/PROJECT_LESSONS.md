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
