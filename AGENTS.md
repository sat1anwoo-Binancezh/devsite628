# Workspace Instruction

Use `C:\Users\Lenovo\.local\bin\rtk.exe` as the default wrapper for noisy shell output in this workspace to reduce token waste.

Start substantial work with low-noise orientation such as:

```powershell
C:\Users\Lenovo\.local\bin\rtk.exe deps
C:\Users\Lenovo\.local\bin\rtk.exe git status
```

Prefer RTK wrappers for searches, file previews, git status/diff/log, test failures, lint/typecheck/build output, logs, and JSON summaries. Use `Get-ChildItem`, `rg --files`, or `git ls-files` for directory orientation on Windows unless GNU tools are installed. Use raw shell commands when exact full output is necessary.

## Superpowers Skill Routing

Use the installed Superpowers skills as lightweight workflow gates, not as automatic token-heavy ceremony.

- For multi-step feature work, use `writing-plans` before editing and `executing-plans` when following a written plan.
- For bugs, failed tests, blank screens, packaging failures, or unexpected behavior, use `systematic-debugging` before proposing fixes.
- For behavior changes with meaningful risk, use `test-driven-development` when a focused test is practical.
- Before claiming a task is complete, fixed, or ready, use `verification-before-completion` and report the fresh evidence.
- For isolated feature work or risky branches, consider `using-git-worktrees`; skip it for small local fixes.
- For major finished work, use `requesting-code-review` / `receiving-code-review` when review is requested or useful.
- Do not load `using-superpowers` on every trivial response; treat it as a reference for skill routing when the workflow itself is unclear.
