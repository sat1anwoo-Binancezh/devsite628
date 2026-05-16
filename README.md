# devsite628

联合开发署备份传送点。

Codex development headquarters for skills, web apps, tools, docs, and experiments.

## Layout

- `AGENTS.md` - workspace rules for Codex collaboration.
- `skills/` - source copies of custom skills that should be backed up and maintained.
- `web-apps/` - web applications and prototypes.
- `tools/` - local scripts, CLIs, and automation utilities.
- `docs/` - project notes, architecture records, and operating docs.
- `archives/` - small retained artifacts worth versioning.

## Local Policy

Do not commit secrets, `.env` files, dependency folders, build outputs, or temporary extraction folders.

Installed global Codex skills live outside this repo at `C:\Users\Lenovo\.codex\skills`. Keep source copies here only for skills that should be backed up or edited as project assets.
