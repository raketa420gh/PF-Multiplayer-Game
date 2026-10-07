# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working Style

- Think in English, answer in Russian
- Treat the user as a senior Unity/C# expert — no hand-holding
- Terse responses; give actual code, not high-level descriptions
- Fewer lines of code = better
- Do not stop until task is complete
- When fixing bugs: explain the problem in plain Russian, make minimal changes
- Any question to the user must go through `AskUserQuestion` with options to pick from — never ask open-ended questions that require a typed answer

## Token Economy

- Context is re-read on every call — keep sessions short. When a task spans several unrelated items, finish one, then suggest `/clear` before the next
- Locate code with `Grep` (`-C` context) and read only the needed range (`offset`/`limit`); read whole files only when editing most of them
- Use Read/Grep/Glob, not `cd … && sed/cat/grep` in Bash
- Batch independent tool calls in one message
- Delegate broad exploration and self-contained sub-tasks to subagents — keep only their conclusions in the main context
- Screenshots: at most 1–2 per verification, width ≤ 1280 px; prefer numeric checks via `execute_code` over images
- Verify Unity changes with one `execute_code` call — `return Game.Scripts.Editor.RebuildVerify.Run("Verify");` (modes: `Verify`, `Battle`, `Dungeon`, `All`) — instead of refresh → read_console → execute_code loops. After a rebuild that recompiles, run `Verify` once more

## Code Style

- Public events and properties come first, then private
- Method order: Unity methods → public → private → EventHandlers
- After `[SerializeField]` (and other attributes), next text on a new line
- No `public Var { get; private set; }` — always `public Var => _var;` + setter method
- Access modifiers must always be explicitly specified — no implicit `private` or `internal`. Every field, method, property, constant, and class member must have an explicit access modifier (`private`, `public`, `protected`, `internal`, etc.)
- SOLID, OOP, GRASP, KISS
- All toggleable Views must inherit from `DisplayableView` (`Show()` / `Hide()` methods)
- Never use `FindObjectsOfType`, `FindObjectOfType`, `FindObjectsByType`, `GameObject.Find`, `GameObject.FindWithTag` or any other scene-search Unity APIs — use dependency injection, serialized references, or service locator instead
- ScriptableObject naming: avoid `SO` suffix — use `Config` instead (e.g. `EnemyConfig`, not `EnemySO`)
- Zenject DI: never use field injection (`[Inject] private Foo _foo`). Always inject via method: `[Inject] public void Construct(Foo foo) => _foo = foo;`
- Always insert a blank line before `return;` (separate the return statement from the preceding code)

