# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working Style

- Think in English, answer in Russian
- Treat the user as a senior Unity/C# expert — no hand-holding
- Terse responses; give actual code, not high-level descriptions
- Fewer lines of code = better
- Do not stop until task is complete
- When fixing bugs: explain the problem in plain Russian, make minimal changes

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

