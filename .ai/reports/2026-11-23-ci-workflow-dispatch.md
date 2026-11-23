# CI manual trigger and Actions ledger — 2026-11-23

Question: can CI be started from the Actions tab or API, and what evidence belongs on OA-1?

## Change

`.github/workflows/ci.yml` gained `workflow_dispatch` with no inputs. The existing `pull_request` and `push` to `stable` triggers are unchanged. No job step, `release.yml`, application code, or test was edited.

## YAML

`python3` `yaml.safe_load` parsed `.github/workflows/ci.yml` into a mapping. PyYAML 1.1 loads the unquoted key `on` as boolean true; under that key the triggers are `pull_request` and `push` to `stable`, plus `workflow_dispatch` with no inputs. That answers "does this file parse as YAML". It does not answer whether a GitHub Actions run completes.

## OA-1

Status remains `open`. The Actions permissions API returned HTTP 403, so `enabled` was not read. Evidence records the 2026-09-25 removal (PR #29, `5fe575d`), the 2026-11-22 restore with no push or pull_request run (last earlier run: `test.yml` on 2026-08-30, run 33300449485), and the 2026-11-23 Release dispatch https://github.com/linjingzhu/video-player/actions/runs/45159337219 (queued when read, actor linjingzhu, head `65ae28a`). Paid automation approval (workflow, scope, provider, max cost, expiry) is still pending and was not recorded as granted.

NOT VERIFIED: a completed CI run; the repository Actions enabled flag; playback and Windows runtime.
