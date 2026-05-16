---
name: rtk-token-economy
description: Use RTK, Rust Token Killer, to reduce token waste during codebase exploration, file reads, searches, diffs, git inspection, test output, lint output, package-manager output, logs, and other verbose shell output. This skill should be used at the start of substantial work in this workspace and whenever command output may be large.
metadata:
  short-description: Token-efficient shell output with RTK
---

# RTK Token Economy

Use RTK before spending context on noisy command output.

## Binary

Preferred executable:

```powershell
C:\Users\Lenovo\.local\bin\rtk.exe
```

If `rtk` is on PATH, either `rtk` or the absolute path is fine. If PATH lookup fails, use the absolute path.

## Required Habit

At the start of substantial work in this workspace, use RTK for low-noise orientation before broad raw reads:

```powershell
C:\Users\Lenovo\.local\bin\rtk.exe deps
C:\Users\Lenovo\.local\bin\rtk.exe git status
```

Use raw commands when exact bytes, full files, or unfiltered logs are required.

## Command Mapping

Prefer these wrappers for potentially verbose output:

- File reading: `rtk read path\to\file`, `rtk smart path\to\file`
- Search: `rtk grep "pattern" .`
- Git: `rtk git status`, `rtk git diff`, `rtk git log -n 10`
- Tests: `rtk test -- <command>`, or specialized commands like `rtk pytest`, `rtk vitest`, `rtk jest`, `rtk cargo test`
- Type/lint/build output: `rtk tsc`, `rtk lint`, `rtk next build`, `rtk npm run build`, `rtk pnpm test`
- Logs/JSON: `rtk log`, `rtk json`, `rtk pipe`

On this Windows PowerShell setup, do not assume `rtk ls`, `rtk tree`, or `rtk find` are available because RTK proxies native Unix-style commands. Use `Get-ChildItem`, `rg --files`, or `git ls-files` for directory orientation unless GNU tools are installed.

## Validation

Before relying on the tool after install or update:

```powershell
C:\Users\Lenovo\.local\bin\rtk.exe --version
C:\Users\Lenovo\.local\bin\rtk.exe gain
```

`rtk gain` must exist; otherwise it may be the wrong package.

## References

- `references/README.md`: upstream overview and command catalog.
- `references/INSTALL.md`: upstream installation and initialization notes.
- `references/filters.toml`: sample project-local RTK filters.
