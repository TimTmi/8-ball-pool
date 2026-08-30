# Context Guide: 8 Ball Pool

This folder is the lightweight memory pack for AI agents working on this project. Keep files short, factual, and current. Prefer links between files over repeating the same details.

Available skills:
- `init-context`: Initialize this folder on a new project
- `use-context`: Read and use this folder effectively during development
- `update-context`: Update specific context files after completing work
- `review-context`: Audit context files for staleness

## Read Order

For a new conversation, read:
1. `project_brief.md` for scope, stack, and core features
2. `progress.md` for current state and next task
3. `coding_rules.md` before changing code

Then read only what the task needs:

| Task type | Read these files |
| --- | --- |
| Architecture/backend design | `design.md`, `decisions.md` |
| Feature flow details | `design.md`, `decisions.md` |
| Planning/prioritization | `roadmap.md`, `progress.md` |

## File Roles

- `project_brief.md`: concise product summary, stack, and must-have features
- `design.md`: system architecture, component responsibilities, communication flows, and reliability patterns
- `coding_rules.md`: implementation standards and agent behavior rules
- `roadmap.md`: planned build order
- `progress.md`: completed work, current work, blockers, next steps
- `decisions.md`: accepted technical decisions and important tradeoffs

*Note: `original_requirements.md`, `data_models.md`, and `api_spec.md` are omitted as they are not needed for this straightforward local game project.*

## Maintenance Rules

- Update `progress.md` after each meaningful milestone
- Update `design.md` only for architecture-level changes
- Add to `decisions.md` only for non-obvious decisions that future agents might re-debate
- Add a new `.context` file only when a feature flow is complex enough that adding it to an existing file would bloat the core docs

## Context Budget

- Keep `project_brief.md`, `progress.md`, and `roadmap.md` under about 1 page each
- Avoid copying full requirements into other files
- Put implementation details in code, not context docs, unless agents need the rule before coding
- Remove stale plans once implementation reality differs from them
