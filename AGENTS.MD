# AGENTS Guide

## Branch Workflow

- Protected branches for this repository are `main`, `stage`, and `dev`.
- Code changes must be tied to a GitHub issue.
- If no GitHub issue exists for requested code work, create one or request one before implementation begins.
- Before making code changes, confirm the current branch is not a protected branch.
- If currently on a protected branch, switch to or create an appropriate feature branch before editing code.
- Feature branches must open pull requests into `dev`.
- `stage` can only receive pull requests from `dev`.
- `main` can only receive pull requests from `stage`.
- Before merging, pull the latest remote branch changes into the local branch.
- Resolve merge conflicts locally before completing the merge or opening the final pull request.

## Implementation Standards

- Use coding best practices when implementing changes.
- Pay close attention to SOLID principles when designing or modifying code.
- Do not store API keys, usernames, passwords, or configuration variables containing them anywhere other than app secrets.
- Fix build warnings when they are discovered.
- Create unit tests for new features.
- After completing each assigned task, run the relevant tests before committing changes.
- Commit completed task changes once verification is finished.

## Communication And Scope

- When the user asks for a task to be completed, ask clarification questions instead of making assumptions unless the user explicitly gives creative control.
- Keep the user informed of the current number of unpushed commits when work is in progress or after new commits are created.
- Keep the current assignment in scope.
- If the user shares an idea that is outside the scope of the current assignment, add it to `TODO.MD` in the project root for later follow-up.

## Pull Request Commands

- Treat `Promote Dev to Main`, `Promote Dev to Prod`, and equivalent wording as a promotion command that should move changes through every required repository step, not just the first branch hop.
- For promotion commands targeting `main` or production, follow the protected-branch path automatically in order, including `dev -> stage` and then `stage -> main`, unless the user explicitly narrows the request.
- Treat `PR auto <number>` as a pull request automation command, not as the issue-number conversation opener workflow.
- When a conversation starts with `PR auto ...`, do not create a fresh feature branch from `dev` just because the command includes a number. Operate on the referenced pull request instead.
- The number passed to `PR auto` is the pull request number even if the user casually refers to it as an issue number.
- Ignore optional filler tokens and markers in the shorthand. `PR auto 137`, `PR auto for 137`, `PR auto #137`, and `PR auto for #137` all mean the same thing.
- For `PR auto`, resolve the number as a pull request first. Only ask whether the user meant an issue if that pull request number does not exist in this repository.
- Treat `PR <number>` and `PR #<number>` without `auto` as pull request inspection requests by default, not as issue work and not as full automation requests.
- `PR auto` means to run the pull request follow-up workflow for that PR: inspect status, address actionable review or CI feedback, run relevant validation, commit and push fixes, request fresh Codex review, and continue monitoring with the required heartbeat automation while the PR remains in review.
- `PR auto` alone does not authorize merging. Do not complete or merge the pull request unless the user explicitly asks to do that and the repository review rules allow it.

## Knowledge Capture

- When errors require a workaround to complete a user task, document the workaround in `AGENTS.MD` so future tasks can be completed faster.
- When `gh api` payloads become fragile in PowerShell because of quoting, prefer direct `gh` commands or write JSON to a temporary file before calling the API.
- If the GitHub `fetch_comments.py` helper fails on Windows with a `charmap`/Unicode decode error, rerun it with UTF-8 forced, for example `PYTHONUTF8=1 python fetch_comments.py`.
- Avoid running `dotnet build` and `dotnet test` in parallel against the same solution or projects on Windows, because file locks in `bin/obj` can cause build failures.
- If `dotnet test` cannot copy the server build outputs because `ProjectPortfolio2026.Server.dll` or `ProjectPortfolio2026.Server.exe` is locked by Visual Studio or a running server process, rerun the test command with `-c Release` to use the unlocked Release output path.
- If `dotnet ef` is unavailable when generating Entity Framework migrations, install the matching `dotnet-ef` tool version first before retrying the migration command.
- When the .NET coverage collector writes empty Cobertura files under a `TestResults` directory on Windows, switch the coverage script to a different results folder name such as `CoverageResults`.
- If PowerShell cannot resolve `node` or `npm` on Windows, verify that the Node.js toolchain is installed before attempting frontend dependency changes or client build verification.
- If Visual Studio deployment for the `.esproj` fails with `Value cannot be null. Parameter name: source` while referencing `launch.json`, restore or recreate `projectportfolio2026.client/.vscode/launch.json`; the JavaScript project template expects that debug profile to exist.
- When opening this repository in Visual Studio on this machine, prefer Visual Studio 2026 Insiders at `C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe` instead of the default Visual Studio 2022 instance.
- If the development app fails to start because LocalDB is still attached to an `.mdf` under an older Codex worktree path, relaunch with an explicit `ConnectionStrings__PortfolioDatabase` environment override that points `AttachDbFilename` at the current worktree's `ProjectPortfolio2026.Server\App_Data` folder and uses a unique database name for that worktree.
- When a pull request is ready for review and the ChatGPT Codex Connector has not started reviewing yet, add a top-level `@codex review` comment on the PR to bring the connector's attention to it. Avoid repeating the ping if that comment is already present for the current review cycle.
- Use `@codex review` only to trigger Codex Connector review attention. Do not use `@codex address that feedback` or other instruction-like Codex comments in this repository's PR workflow.
- When review feedback has been addressed with a code or workflow change, mark the corresponding GitHub review thread as resolved so the next Codex review cycle sees the thread as handled.
