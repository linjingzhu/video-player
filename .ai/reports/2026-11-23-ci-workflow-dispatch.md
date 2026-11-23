# CI manual trigger and Actions ledger — 2026-11-23

Question: can CI be started from the Actions tab or API, and what evidence belongs on OA-1?

## Change

`.github/workflows/ci.yml` gained `workflow_dispatch` with no inputs. The existing `pull_request` and `push` to `stable` triggers are unchanged. No job step, `release.yml`, application code, or test was edited.

## YAML

`python3` `yaml.safe_load` parsed `.github/workflows/ci.yml` into a mapping. PyYAML 1.1 loads the unquoted key `on` as boolean true; under that key the triggers are `pull_request` and `push` to `stable`, plus `workflow_dispatch` with no inputs. That answers "does this file parse as YAML". It does not answer whether a GitHub Actions run completes.

## OA-1

Status remains `open`. The Actions permissions API returned HTTP 403, so `enabled` was not read. Evidence records the 2026-09-25 removal (PR #29, `5fe575d`), the 2026-11-22 restore with no push or pull_request run (last earlier run: `test.yml` on 2026-08-30, run 33300449485), and the 2026-11-23 Release dispatch https://github.com/linjingzhu/video-player/actions/runs/45159337219 (actor linjingzhu, head `65ae28a`). As of 2026-11-23 19:53 KST that run was still queued with 0 jobs, and opening PR #51 created no pull_request runs. Read again at 19:54 KST: run status queued, jobs total_count 0, pull_request runs since 2026-11-23 total_count 0, check runs on `1d2ec1f` total_count 0. Paid automation approval is recorded on OA-2.

## OA-2

Status `approved`. On 2026-11-23 about 19:53 KST, owner jeongsu (linjingzhu) said "승인합니다. 만료일은 2026-12-31입니다." Workflows: CI (`.github/workflows/ci.yml`) and Release (`.github/workflows/release.yml`). Repository: linjingzhu/video-player. Provider: GitHub-hosted runner windows-latest. Expected maximum cost: 0 (free tier only; any charge is out of scope). Expiry/review date: 2026-12-31. Workflow steps were not edited.

NOT VERIFIED: a completed CI run; the repository Actions enabled flag; playback and Windows runtime. A fail-closed budget or expiry check inside the workflows was not added.
