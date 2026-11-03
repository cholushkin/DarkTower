# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Dark Tower is a Unity fantasy time-management/strategy game. Unity version `6000.4.8f1`. The Unity
project root is `Unity/Game/` (open this folder in Unity / Unity Hub, not the repo root).

## Primary sources of truth

Read these before making gameplay or architecture changes — they are the design contracts, not just
background reading:

- `Unity/Game/Assets/Game/Documentation/DarkTowerGameDesign.md` — gameplay mechanics, rules,
  progression, core loop. Rules stated as facts are the intended design; ambiguous/unspecified
  behavior is listed under its "Open Design Decisions" section. **Do not invent permanent gameplay
  rules to fill an open decision — flag the ambiguity instead.**
- `LocalPackages/rmvvm/Documentation~/rMVVM.md` — reactive MVVM architecture (Model / ViewModel /
  Composer / View responsibilities, R3 state semantics, DI lifetime rules).
- `LocalPackages/rmvvm/Documentation~/ReactiveWindowSystem.md` — window/navigation system contract
  (container mappings, stack modes, modal lifecycle).
- `LocalPackages/rmvvm/Documentation~/DataFlow.md` — state initialization, ownership, VContainer
  scopes, optional persistence.

These three rMVVM documents describe a **reusable architecture package**: they must never gain
project-specific types, content IDs, or dependencies on `DarkTower.*` assemblies. Game code depends
on rMVVM; rMVVM must never depend on game code.

## Local packages

`LocalPackages/` holds packages consumed via `file:` UPM references (see
`Unity/Game/Packages/manifest.json`) — each is its own git repo (nested `.git`), **not** a vendored
read-only dependency:

- `gamelib` (`com.qcat.gamelib`) — general reusable Unity framework: DI/VContainer support, state
  machine, scheduler-adjacent helpers, input, logging, randomness, editor tooling (hierarchy,
  toolbar, project colors).
- `rmvvm` (`com.qcat.rmvvm`) — the reactive MVVM + window system described above.
- `dev-conventions` (`com.qcat.dev-conventions`) — documentation-only package (no code); see Coding
  conventions below.
- `VersionHistory` (`com.qcat.versionhistory`) — changelog/version-history UI package.

You are explicitly allowed to modify, extend, refactor, and fix these packages when a task needs it.
Prefer improving an existing local package over duplicating its functionality inside
`Unity/Game/Assets/Game`. Preserve backward compatibility for their public APIs where practical.

## Architecture (game code)

Game code lives under `Unity/Game/Assets/Game/`. Current assemblies, in reference order
(`Data` → `Domain` → `Gameplay`; a lower layer never references a higher one):

- `DarkTower.Data` (`Runtime/DarkTower/Data/`) — designer-authored `ScriptableObject` definitions
  (`DarkTowerDatabase` is the master catalog, with cached ID-based lookups) and plain data
  records/enums. No dependencies beyond Unity; never references `DarkTower.Domain` or
  `DarkTower.Gameplay`.
- `DarkTower.Domain` (`Runtime/DarkTower/Domain/`, references `DarkTower.Data`) — runtime session
  state ownership and initial-state construction:
  - `Models/` — authoritative runtime session state (`GameSessionModel` and sub-models such as
    `EconomyModel`, `MinionModel`, `WorldModel`, `TowerModel`, etc.), all deriving from `ModelBase`
    (owns an R3 `CompositeDisposable`; disposal cascades from the session root). Write methods are
    `internal`, visible only to `DarkTower.Gameplay`/`DarkTower.Gameplay.Tests`
    (`InternalsVisibleTo` grants in `Domain/AssemblyInfo.cs`) — a presentation layer can only read.
  - `Factory/SessionStateFactory.cs` — builds a fresh session's starting runtime state from config.
  - Never references `DarkTower.Gameplay`.
- `DarkTower.Gameplay` (`Runtime/DarkTower/Gameplay/`, references `DarkTower.Data` +
  `DarkTower.Domain` + `VContainer`) — the service layer:
  - `Services/<Area>/` (`Core`, `Economy`, `Minions`, `Dungeon`, `Laboratory`, `Payroll`, `Tower`,
    `World`) — operations, validation, and coordinated mutation of Models. This is where gameplay
    rules from `DarkTowerGameDesign.md` get implemented.
  - `DependencyInjection/` — one `*Installer` per area, composed by `GameInstallers.RegisterAll`.
    `GameSessionLifetimeScope` is the real Unity scene composition root (single VContainer session
    scope — no per-feature child scopes). `GameplaySessionComposer`/`GameplaySession` build the same
    container **headlessly** (no scene, no `LifetimeScope`) for tests and tools, resolving every
    service and running `GameSessionStartup` manually.
  - `GameSessionStartup` — explicit post-construction init step (initial bed auto-assignment,
    mission-accessibility refresh), run via VContainer `IInitializable` in-game or called directly in
    the headless path.
  - `Events/DomainEvents.cs` — VitalRouter command/event types for decoupled cross-feature
    notifications (see rMVVM.md §4 for when to use direct calls vs. VitalRouter).
- No presentation/UI/ViewModel layer exists yet in `Assets/Game` — only `Data`, `Domain`, and
  `Gameplay`. When adding UI, follow rMVVM.md/ReactiveWindowSystem.md and keep it out of these three
  assemblies.
- `Editor/DarkTower/` (`DarkTower.Data.Editor`, references `DarkTower.Data` only) — editor-time
  tooling, e.g. `DarkTowerDataValidator` for catalog integrity checks.
- `Tests/DarkTower/EditMode/` (`DarkTower.Gameplay.Tests`, references `DarkTower.Data` +
  `DarkTower.Domain` + `DarkTower.Gameplay`) — NUnit EditMode tests exercising the headless
  `GameplaySessionComposer` path; organized one file per gameplay area plus
  `EndToEndScenarioTests`, `IntegrityTests`, and `GameplayTestFixtures` for shared setup.

Keep domain/gameplay logic (Models, Services) independent of any future UI/presentation code.

## Commands

This is a Unity project with no CLI build/test scripts checked in; use the Unity Editor or Unity's
command-line batch mode.

- **Open the project:** open `Unity/Game/` in Unity Hub (requires Editor `6000.4.8f1`).
- **Run tests (Editor):** Window → General → Test Runner → EditMode tab → Run All, or run a single
  test by right-clicking it in the tree.
- **Run tests (CLI, EditMode):**
  ```
  <Unity editor path> -batchmode -projectPath "Unity/Game" -runTests -testPlatform EditMode -testResults results.xml -testFilter <FullyQualifiedTestName>
  ```
  Omit `-testFilter` to run the full `DarkTower.Gameplay.Tests` suite.
- **Editor-time catalog validation:** `DarkTowerDataValidator` (Editor assembly) — run from its menu
  item in the Unity Editor after editing `DarkTowerDatabase` content.

## Coding conventions

From `LocalPackages/dev-conventions/Documentation/CodingConvention.md` (project-wide, applies to all
game and local-package code):

- Private/protected fields: `_camelCase`. Properties/public members: `PascalCase`. Constants:
  `SCREAMING_SNAKE_CASE`; `PascalCase` for static readonly config-like types.
- Class suffixes: `Service` for cross-cutting/domain logic, `Controller` for a single actor/system's
  state machine, `Processor` for pure data transforms. Avoid `Manager`/`Util` — name by role
  (`Repository`, `Registry`, etc.).
- No public mutable fields — use properties (`[field: SerializeField]` for inspector-exposed
  auto-properties).
- File-scoped namespaces. Allman braces; single-line `if`/loop bodies omit braces (2-line form).
  120-char soft line length.
- Prefer self-documenting names over comments/XML doc blocks; reserve XML docs for shared
  libraries or genuinely non-obvious algorithms. **Comments must describe the current system and its
  intent — never task numbers, agent/work history, or design-doc section numbers.**
- Guard clauses / early returns over nested conditionals. Avoid allocations (LINQ, boxing,
  `Instantiate`) in `Update`/`FixedUpdate`/hot paths.

From `ProjectConvention.md`: keep runtime/editor code separated by asmdef (`DarkTower.Data` never
references `UnityEditor`); no dumping-grounds folders (`Managers`/`Helpers`/`Utils`/`Misc`); prefer a
flat, feature-first folder structure.
