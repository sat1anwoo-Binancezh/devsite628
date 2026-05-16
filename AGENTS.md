# Workspace Instruction

Use `C:\Users\Lenovo\.local\bin\rtk.exe` as the default wrapper for noisy shell output in this workspace to reduce token waste.

Start substantial work with low-noise orientation such as:

```powershell
C:\Users\Lenovo\.local\bin\rtk.exe deps
C:\Users\Lenovo\.local\bin\rtk.exe git status
```

Prefer RTK wrappers for searches, file previews, git status/diff/log, test failures, lint/typecheck/build output, logs, and JSON summaries. Use `Get-ChildItem`, `rg --files`, or `git ls-files` for directory orientation on Windows unless GNU tools are installed. Use raw shell commands when exact full output is necessary.
