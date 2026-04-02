# AGENTS Guide

## Branch Workflow

- Protected branches for this repository are `main`, `stage`, and `dev`.
- Code changes must be tied to a GitHub issue.
- If no GitHub issue exists for requested code work, create one or request one before implementation begins.
- Before making code changes, confirm the current branch is not a protected branch.
- If currently on a protected branch, switch to or create an appropriate feature branch before editing code.
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

## Knowledge Capture

- When errors require a workaround to complete a user task, document the workaround in `AGENTS.MD` so future tasks can be completed faster.
- When `gh api` payloads become fragile in PowerShell because of quoting, prefer direct `gh` commands or write JSON to a temporary file before calling the API.
