# Stable merge integration — 2026-11-22

Question: does `474eb43` (stable after PRs #39–#48) build, pass `tests/VideoPlayer.Tests`, and keep the Pack B menu and key contract?

## Build

`dotnet build VideoPlayer.sln -c Release -p:EnableWindowsTargeting=true` at `474eb43` failed: 7 errors, 0 warnings, all in `tests/VideoPlayer.Smoke/Program.cs` (`Path` and `File` unresolved). `UseWPF` removes the implicit `System.IO` using. App and Tests had already compiled.

After `using System.IO`, the same command succeeded: 0 warnings, 0 errors. Smoke was not executed (Windows / libmpv).

## Tests

`dotnet test tests/VideoPlayer.Tests/VideoPlayer.Tests.csproj -c Release` before and after the fix: Passed 314, Failed 0, Skipped 0, Total 314.

## Pack B menus and keys

Both context menus are the same XAML. `MenuOrder` puts the quick block first and the view block last. No duplicate top-level keys. One `OnPreviewKeyDown`. Right-click opens the quick-menu instance; the hamburger opens the other copy. Both are updated by the same hooks.

## CI

`.github/workflows/ci.yml` triggers on push and pull_request to `stable`, which is the default branch. Workflow `CI` is `active` and has 0 runs. HEAD `474eb43` has 0 check runs. The public Actions tab returned HTTP 404. Last run in the repository is `test.yml` on 2026-08-30. Owner action: OA-1. Workflow files were not changed.
