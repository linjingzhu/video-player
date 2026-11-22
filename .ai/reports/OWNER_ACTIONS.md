# Owner Actions

| Id | Action | Status | Evidence |
| --- | --- | --- | --- |
| OA-1 | Enable GitHub Actions for this repository. `.github/workflows/ci.yml` and `release.yml` are already on `stable`; do not edit them to force a run. | open | On 2026-11-22, workflow `CI` (id 426788975) was `active` with 0 runs after the stable pushes through `474eb43`. The public Actions tab returned HTTP 404. The last recorded run is `test.yml` on 2026-08-30. The permissions API was not readable with this token (HTTP 403), so `enabled: false` was not read directly. |
