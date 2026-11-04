# SceneFlow in Dark Tower

How Dark Tower composes its scenes with the `GameLib.SceneFlow` module. This document is
project-specific: it describes Dark Tower's actual scene graph, the desired steady-state flow, and
where that flow is still incomplete. For the reusable mechanics (assets, runtime algorithm,
validation, toolbar) see the module's own documentation at
`LocalPackages/gamelib/Runtime/SceneFlow/Documentation/SceneFlow.md` — this file does not repeat
that material, only how Dark Tower uses it.

## Current scene graph

Four scenes under `Assets/Game/Scenes/`:

| Scene | Role | DI scope | SceneDefinition dependency |
|---|---|---|---|
| `Boot.unity` | Native, non-Addressable entry scene. Holds the one `SceneFlowBootstrap` (the execution host `LifetimeScope`). Assigned as `SceneFlowSettings.BootstrapScene`. | Host scope (SceneFlow infrastructure only) | n/a — not Addressable, not a `SceneDefinition` |
| `System.unity` | Global, scene-level content with no gameplay state: main camera, directional light, input, dev GUI, global volume. | None — no `LifetimeScope` in the scene | `RequiredScenes: []` (no dependencies) |
| `Game.unity` | The gameplay session. Holds `GameSessionLifetimeScope`, the single VContainer session scope (per `DataFlow.md` §5 — no per-feature child scopes). `GameInstallers.RegisterAll` registers `Config`, `Core`, and every `DarkTower.Gameplay` service here; `GameSessionStartup` runs as its entry point. | `GameSessionLifetimeScope` | Requires `System` |
| `BaseUI.unity` | The rMVVM window host: `WindowService(Canvas)`, `MainScreen`, `ModalOverlay`, `BackgroundScreen`, `SystemMessages`, `FlyingUIContainer` (see `ReactiveWindowSystem.md`). | None yet — no `LifetimeScope` in the scene | Requires `Game` |

One `RunProfile` exists today, `DefaultPlay` (`DataAndSettings/SceneFlow/RunProfile/DefaultPlay.asset`):
requests `System`, `Game`, `BaseUI`, sets `ActiveScene = Game`, no `Installers`, no
`ParentOverrides`. It is `SceneFlowSettings.DefaultProfile`, so it is what both an ordinary Editor
Play and a player build run.

Combining `RequiredScenes` across the three `SceneDefinition`s gives one strict chain, so
`SceneOrchestrator` always loads them in this order regardless of the order they're listed in the
profile:

```
System  →  Game  →  BaseUI
(no deps)  (needs    (needs
            System)   Game)
```

### DI parenting today

With `ParentOverrides` empty, both `System` and `BaseUI` parent to the **execution host** (Boot's
`SceneFlowBootstrap` scope) — not to `Game`. That host registers nothing beyond SceneFlow's own
plumbing. This is fine for `System` (it has no services to resolve) but is an **open item** for
`BaseUI`: once windows under `BaseUI` grow ViewModels that need to resolve `DarkTower.Gameplay`
services, `BaseUI` must gain an explicit `ParentOverrides` entry in `DefaultPlay` pointing at `Game`,
so its composition parents to `GameSessionLifetimeScope` instead of the empty host. Until that
override is added, nothing in `BaseUI` can resolve a gameplay service through DI. See `rMVVM.md` for
how a presentation-layer child scope is expected to consume the session scope.

## Desired flow

1. **Boot** (native scene, not part of any `SceneDefinition` graph) is the only scene ever opened
   directly — by a player build or an ordinary Editor Play. Its `SceneFlowBootstrap` resolves
   `SceneFlowSettings.DefaultProfile` (`DefaultPlay`) and hands it to `SceneOrchestrator`.
2. **System** loads first. It owns engine-level, session-independent objects (camera, light, input,
   dev GUI) and deliberately has no `LifetimeScope` — there is nothing here that should be resolved
   through DI, and nothing here should ever depend on gameplay state.
3. **Game** loads next, additively, parented to the host. `GameSessionLifetimeScope.Configure` runs
   `GameInstallers.RegisterAll` (Config → Core → every service in `DarkTower.Gameplay.Services`) and
   registers `GameSessionStartup` as the entry point, which performs initial bed auto-assignment and
   mission-accessibility refresh exactly as the headless `GameplaySessionComposer` path does for
   tests. From this point the one authoritative `GameSessionModel` exists and every gameplay service
   can be resolved.
4. **BaseUI** loads last, additively. It is the presentation root: `WindowService` plus the screens
   rMVVM's Reactive Window System composes. Per the open item above, once this scene starts hosting
   real ViewModels, its `RunProfile` entry should carry an explicit `ParentOverrides → Game` so its
   composition can resolve the services `GameSessionLifetimeScope` registers. `ActiveScene` stays
   `Game` — SceneFlow only uses it to pick the scene Unity marks active, independent of DI parenting.
5. Nothing in this chain unloads during normal play — Dark Tower does not yet have a scene that gets
   swapped or torn down mid-session. When that need arises (see below), prefer adding a new
   `SceneDefinition`/`RunProfile` over overloading `Game` or `BaseUI`.

## How SceneFlow works, applied to Dark Tower

The concepts below are the module's (full detail in the package doc); this is how each one maps onto
the graph above.

- **`SceneDefinition`** — one per Addressable scene (`System`, `Game`, `BaseUI`). Identity is the
  scene asset's GUID, so renaming a scene file never breaks a reference the way a hand-authored
  string ID would.
- **`RunProfile`** — `DefaultPlay` is Dark Tower's only profile today. A profile is the unit a
  designer/QA picks from the SceneFlow toolbar; adding a second profile (see use cases below) never
  requires new code, only a new asset.
- **`SceneFlowSettings`** — one asset (`DataAndSettings/SceneFlow/SceneFlowSettings.asset`) wires
  `Boot.unity` as `BootstrapScene` and `DefaultPlay` as `DefaultProfile`. `DevelopmentHostScene` is
  currently unset — Dark Tower has not yet needed `▶ Isolated` launches, but the field is reserved
  for the use cases below.
- **`SceneFlowInstaller`** — none exist in Dark Tower yet. `GameInstallers.RegisterAll` is called
  directly from `GameSessionLifetimeScope.Configure`, not through a `SceneFlowInstaller`, because
  `Game` is always loaded as part of the one production profile. A `SceneFlowInstaller` only earns
  its place once there's a reason to swap what gets registered per-launch (isolation, QA, tests) —
  see below.
- **Toolbar / `▶ Profile`** — the normal way to playtest: pick `DefaultPlay`, press ▶, get `System →
  Game → BaseUI` exactly as a build would, from inside the Editor.
- **Editor validation** — run **GameLib/SceneFlow/Validate SceneFlow** after touching any
  `SceneDefinition`/`RunProfile` asset in `DataAndSettings/SceneFlow/`; it uses the same
  `SceneOrchestrator.TryBuildPlan` the runtime does, so a green validation means the graph above is
  guaranteed loadable.

## Future potential use cases and examples

These are **not implemented** — they illustrate where SceneFlow's existing mechanics already cover a
need Dark Tower is likely to have, so a future task can wire them up without inventing new
machinery. None of these imply a design decision beyond "SceneFlow already supports this if asked
for" — see `DarkTowerGameDesign.md`'s Open Design Decisions for anything gameplay-rule-level.

- **Wiring `BaseUI` to the session scope.** The concrete next step: add a `ParentOverrides` entry
  `BaseUI → Game` to `DefaultPlay` as soon as the first ViewModel under `BaseUI` needs to resolve a
  `DarkTower.Gameplay` service. No new `SceneDefinition` needed — only the profile edit.
- **Isolated UI iteration.** Once `BaseUI` has real ViewModels, a UI-focused contributor can iterate
  on it without booting `System`/`Game` at all: assign a `DevelopmentHostScene`, give `BaseUI` a
  small `SceneFlowInstaller` that registers fixture/fake implementations of whatever services its
  ViewModels need (mirroring how `GameplaySessionComposer` builds a headless container for EditMode
  tests), and use **▶ Isolated** from the toolbar. Fast iteration loop, no production session cost.
- **A dedicated dungeon/raid scene.** `DarkTowerGameDesign.md`'s dungeon and raid mechanics could
  justify their own Addressable scene (e.g. `Dungeon.unity`) with a `SceneDefinition` whose
  `RequiredScenes` includes `Game` — loaded additively on top of the running session when a raid
  starts, unloaded when it ends, instead of folding heavy raid-only content into `Game` permanently.
  `SceneOrchestrator`'s dependency-checked unload already rejects unloading `Game` while `Dungeon`
  (or anything else) still requires it, so this composes safely with the existing graph.
- **A QA/debug profile.** A second `RunProfile` (e.g. `QAPlay`) requesting the same three scenes plus
  an extra debug-overlay `SceneDefinition`, selectable from the toolbar's profile dropdown with zero
  code changes — useful for a cheat/inspector overlay that should never ship in `DefaultPlay`.
- **Content-pack scenes.** Addressables already supports remote/downloadable content; a new
  Laboratory-, event-, or expansion-specific scene can be added as its own `SceneDefinition` and
  folded into a `RunProfile` (or a profile of its own) without touching `System`/`Game`/`BaseUI`.

## Open items

- `BaseUI` has no `LifetimeScope` and no `ParentOverrides` yet — flagged above, not yet a problem
  only because no ViewModel exists under it today.
- No `SceneFlowInstaller` exists in the project yet; `GameInstallers` is called directly because
  there is exactly one production composition. Introduce one only when a second composition (QA,
  isolation, tests-in-Editor) actually needs to differ.
- `SceneFlowSettings.DevelopmentHostScene` is unset — no scene is configured for `▶ Isolated` yet.
