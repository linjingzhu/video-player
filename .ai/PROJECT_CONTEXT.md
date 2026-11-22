---
doc_id: ai-project-context
version: 1.0.0
canonical_path: .ai/PROJECT_CONTEXT.md
updated: 2026-09-25
---

# video-player Context

Repository: linjingzhu/video-player. Evidence baseline: 88511907dae97ebc69e4ef5833565e904032b4af.

## repository_mode

```text
repository_mode: protected
```

## Facts the checks read

```text
base_branch: stable
merge_deploys: yes
runtime_gate: none
test_command: dotnet test tests/VideoPlayer.Tests/VideoPlayer.Tests.csproj
lint_command: none
build_command: dotnet build src/VideoPlayer.App/VideoPlayer.App.csproj -c Release
generated: .NET bin/ and obj/ outputs from application source : dotnet build src/VideoPlayer.App/VideoPlayer.App.csproj -c Release
external_scripts: unverified; inspect the affected product surface before changing script loading
public_ids: unverified; no new public-identifier allowlist established by this policy update
owner_ledger: .ai/reports/OWNER_ACTIONS.md
```

`merge_deploys: yes` is a conservative assumption because external deployment integrations have not been verified. It does not assert that a deployment is configured. GitHub Actions workflows are removed at the owner's request; do not recreate or re-enable them without a new instruction. Existing README/CI references may describe the previous setup; verification now runs locally. A value of `none` above means no applicable verified command was established, not a passed check.

## Authoritative product constraints

Windows desktop player 이어서 plays local files and plain HTTP(S) URLs using libmpv/FFmpeg, with a documented Korean shell, subtitle, resume and series experience.

No store, DRM, accounts, cookies or login. Casting uses the Windows wireless-display picker only; exclude custom receivers, DLNA, Chromecast and AirPlay. URL saving is a plain GET of the same URL without auth/cookies/headers/range or HLS key unwrapping. ProRes, DNxHD, camera RAW, encrypted WMV and DVD/ISO are excluded. Preserve all detailed README shell, subtitle, seek and resume rules.

## Current architecture

VideoPlayer.sln with src/VideoPlayer.App and tests/VideoPlayer.Tests. README specifies Windows application builds, libmpv-2.dll placement beside Ieseo.exe, D3D11VA/DXVA acceleration with software fallback, and the full UI contract.

## Current development slice

Adopt the shared ai-dev-rule 3.0.0 policy and Codex/Claude capabilities and remove tracked GitHub Actions workflows. Application implementation and package/build configuration are unchanged. Later application work must consult current source, README and the retained project documentation rather than infer a feature roadmap from this maintenance slice.

## Permanently excluded scope

Keep the product boundaries stated above and in the repository's product documents. Additional exclusions not established by those sources are unknown; this policy update creates no new product roadmap.

## Repository guidance migration

No previous CLAUDE.md or AGENTS.md was present at the recorded base commit. The shared ai-dev-rule 3.0.0 documents govern generic development workflow, model routing, reporting and merge authority; they supersede contradictory older generic instructions. This does not relax repository-specific product, data or security boundaries. The current explicit user request authorizes this batch policy merge; protected is the safe default for future unrelated work.

A successful .NET build does not verify libmpv playback, device projection, GPU/HDR handling or the Windows shell. No automated full-runtime command was identified in README.

## Verification limits

Policy structural checks and whitespace checks validate the policy update only. Application dependencies, builds, runtime behavior, visual behavior, external deployments, generated-output completeness, external scripts and public identifiers were not fully verified during adoption. Commands above are grounded in tracked README, package/build configuration or prior guidance; they have not been claimed to pass here.
